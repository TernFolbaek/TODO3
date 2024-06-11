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
            _logger.LogInformation("Starting to check and update todo statuses.");
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var currentTimeUtc = DateTimeOffset.UtcNow;

                var todoItems = _context.TodoItems.ToList();
                _logger.LogInformation(
                    "******************************************************************************************************************");
                foreach (var item in todoItems)
                {
                    _logger.LogInformation($"Todo Item: ID={item.Id}, Description={item.Description}, Status={item.Status}, Due Date={item.DueDate}");
                }
                _logger.LogInformation("------------------------------------------------------------------------------------------------------------------");

                var todosToUpdate = _context.TodoItems
                    .Where(t => (t.DueDate < currentTimeUtc && t.Status != "Overdue") ||
                                (t.DueDate >= currentTimeUtc && t.Status == "Overdue"))
                    .ToList();

                _logger.LogInformation($"Evaluated at UTC time: {currentTimeUtc}. Found {todosToUpdate.Count} todos to update.");

                if (todosToUpdate.Any())
                {
                    foreach (var todo in todosToUpdate)
                    {
                        string newStatus = todo.DueDate < currentTimeUtc ? "Overdue" : "Pending";
                        if (todo.Status != newStatus)
                        {
                            _logger.LogInformation($"Changing status for Todo ID {todo.Id} from {todo.Status} to {newStatus}.");
                            todo.Status = newStatus;
                            _context.TodoItems.Update(todo);
                            await _hubContext.Clients.All.SendAsync("ReceiveTodoStatusUpdate", todo.Id, todo.Status);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation($"Successfully updated and notified {todosToUpdate.Count} todos.");
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
            }
        }
    }
}
