using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace TODO.Hubs
{
    // [Authorize] configure with authorization
    public class TodoHub : Hub
    {
        // public async Task SendDueDateUpdate(int todoId, DateTime? dueDate)
        // {
        //     await Clients.All.SendAsync("ReceiveTodoDueDateUpdate", todoId, dueDate);
        // }
        //
        // public async Task UpdateTodoStatus(int todoId, string status)
        // {
        //     Console.WriteLine($"Updating status for TodoId {todoId} to {status}");
        //     await Clients.All.SendAsync("ReceiveTodoStatusUpdate", todoId, status);
        // }
        //
        // public async Task UpdateTodoCompletionDate(int todoId, DateTimeOffset? dateCompleted)
        // {
        //     await Clients.All.SendAsync("ReceiveTodoCompletionDateUpdate", todoId, dateCompleted);
        // }
    }
}