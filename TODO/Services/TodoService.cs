using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TODO.Data;
using TODO.Hubs;

namespace TODO.Services
{
    public class TodoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TodoHub> _hubContext;
        private readonly ILogger<TodoService> _logger;

        public TodoService(ApplicationDbContext context, IHubContext<TodoHub> hubContext, ILogger<TodoService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task CheckAndUpdateTodoStatuses()
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Ensure correct handling of DateTime comparisons by specifying UtcNow explicitly
                var currentUtcNow = DateTimeOffset.UtcNow;
                var todosToUpdate = _context.TodoItems
                    .Where(t => (t.DueDate.HasValue && t.DueDate.Value < currentUtcNow && t.Status != "Overdue") || 
                                (t.DueDate.HasValue && t.DueDate.Value >= currentUtcNow && t.Status == "Overdue"))
                    .ToList();

                if (todosToUpdate.Any())
                {
                    foreach (var todo in todosToUpdate)
                    {
                        // Define new status based on current due date comparison to UTC now
                        string newStatus = todo.DueDate.Value < currentUtcNow ? "Overdue" : "Pending";
                        if (todo.Status != newStatus)
                        {
                            todo.Status = newStatus;
                            _context.TodoItems.Update(todo);
                            await _hubContext.Clients.All.SendAsync("ReceiveTodoStatusUpdate", todo.Id, todo.Status);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation($"Updated and notified {todosToUpdate.Count} todos.");
                }
                else
                {
                    _logger.LogInformation("No todos needed status updates.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error processing todo status updates: {ex.Message}", ex);
                throw;
            }
        }
    }
}
