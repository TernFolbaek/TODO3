using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TODO.Data;
using TODO.Hubs;

namespace TODO.Services
{
    public class TodoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TodoHub> _hubContext;

        public TodoService(ApplicationDbContext context, IHubContext<TodoHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CheckAndUpdateOverdueTodos()
        {
            var overdueTodos = _context.TodoItems
                .Where(t => t.DueDate < DateTime.UtcNow && t.Status != "Overdue")
                .ToList();

            foreach (var todo in overdueTodos)
            {
                todo.Status = "Overdue";
                _context.TodoItems.Update(todo);
                await _hubContext.Clients.All.SendAsync("ReceiveTodoStatusUpdate", todo.Id, todo.Status);
            }

            await _context.SaveChangesAsync();
        }
    }
}
