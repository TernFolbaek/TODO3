using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TODO.Data;
using TODO.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc; 
using Microsoft.AspNetCore.Authorization; 
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using TODO.Hubs;


namespace TODO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class TodoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TodoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<TodoHub> _hubContext;

        public TodoController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<TodoController> logger, IConfiguration configuration, IHubContext<TodoHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        
        // GET: api/todo
        [HttpGet]
        public async Task<IActionResult> GetAllTodoItems()
        {
            try
            {
                var todoItems = await _context.TodoItems.ToListAsync();
                return Ok(todoItems);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching todos : {ex.Message}", ex);
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }


        public record TodoItemRequest(int Id, string Description, DateTime? DueDate, bool IsComplete, List<string> Usernames);
        // POST: api/todo
        [HttpPost]
        public async Task<IActionResult> CreateTodoItem([FromBody] TodoItemRequest todoItemRequest)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => todoItemRequest.Usernames.Contains(u.Username))
                    .ToListAsync();

                if (users.Count != todoItemRequest.Usernames.Count)
                {
                    return BadRequest("One or more usernames do not exist.");
                }

                var todoItem = new TodoItem
                {
                    Id = todoItemRequest.Id,
                    IsComplete = todoItemRequest.IsComplete,
                    Description = todoItemRequest.Description,
                    DueDate = todoItemRequest.DueDate,
                    UserTodos = users.Select(user => new UserTodo { User = user }).ToList(),
                    Status = todoItemRequest.IsComplete ? "Completed" : "Pending",
                    DateCompleted = todoItemRequest.IsComplete ? DateTime.UtcNow : null
                };

                _context.TodoItems.Add(todoItem);
                await _context.SaveChangesAsync();
                return Ok(new {
                    TodoId = todoItem.Id,
                    Description = todoItem.Description,
                    IsComplete = todoItem.IsComplete,
                    DueDate = todoItem.DueDate,
                    Status = todoItem.Status,
                    DateCompleted = todoItem.DateCompleted,
                    UserTodos = todoItem.UserTodos.Select(ut => new { ut.User.Id, ut.User.Username }).ToList() 
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating TODO item: {ex.Message}", ex);
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }


        [HttpGet("{id}")]
    public async Task<IActionResult> GetTodoItem(int id, [FromQuery] string timezone = "UTC")
    {
    // Validate JWT token
    try
    {
        using (var client = _httpClientFactory.CreateClient())
        {
            var response = await client.GetAsync($"http://worldtimeapi.org/api/timezone/{timezone}");
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Failed to retrieve timezone information from WorldTimeApi.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            var utcOffset = ParseUtcOffset(json["utc_offset"].ToString());

            var todoItem = await _context.TodoItems
                .Include(ti => ti.UserTodos) 
                    .ThenInclude(ut => ut.User) 
                .FirstOrDefaultAsync(ti => ti.Id == id); 

            if (todoItem == null)
            {
                return NotFound("Todo item not found.");
            }

            if (todoItem.DueDate.HasValue)
            {
                var localDateTimeOffset = todoItem.DueDate.Value.ToOffset(utcOffset);
                _logger.LogInformation($"Adjusted DueDate: {todoItem.DueDate}");
                todoItem.DueDate = localDateTimeOffset;
            }

            return Ok(new
            {
                TodoId = todoItem.Id,
                Description = todoItem.Description,
                IsComplete = todoItem.IsComplete,
                DueDate = todoItem.DueDate,
                AssignedUsers = todoItem.UserTodos.Select(ut => new 
                {
                    UserId = ut.UserId,
                    Username = ut.User.Username
                }).ToList()
            });
        }
    }
    
    catch (Exception ex)
    {
        _logger.LogError($"An error occurred: {ex.Message}");
        return StatusCode(500, "Internal Server Error: " + ex.Message);
    }
}

    private TimeSpan ParseUtcOffset(string utcOffset)
    {
        if (utcOffset.StartsWith("+") || utcOffset.StartsWith("-"))
        {
            utcOffset = utcOffset.StartsWith("+") ? utcOffset.Substring(1) : utcOffset;
            if (TimeSpan.TryParse(utcOffset, out TimeSpan result))
            {
                return result;
            }
            else
            {
                _logger.LogError($"Failed to parse UTC offset: {utcOffset}");
                throw new FormatException("Invalid time span format");
            }
        }
        else
        {
            _logger.LogError($"Unexpected UTC offset format: {utcOffset}");
            throw new FormatException("Unexpected time span format, expected a leading '+' or '-'");
        }
    }
    public class TodoCompletionRequest
    {
        public bool IsComplete { get; set; }
    }


    // POST: api/todo/toggleCompletion/{id}
    [HttpPost("toggleCompletion/{id}")]
    public async Task<IActionResult> ToggleTodoCompletion(int id, [FromBody] TodoCompletionRequest request)
    {
        var todoItem = await _context.TodoItems.FindAsync(id);
        if (todoItem == null)
        {
            return NotFound();
        }

        todoItem.IsComplete = request.IsComplete;
        if (request.IsComplete)
        {
            todoItem.Status = "Completed";
            todoItem.DateCompleted = DateTime.UtcNow;
        }
        else
        {
            todoItem.Status = "Pending";
            todoItem.DateCompleted = null;
        }

        _context.TodoItems.Update(todoItem);
        await _context.SaveChangesAsync();

        // Send updates via SignalR
        await _hubContext.Clients.All.SendAsync("ReceiveTodoStatusUpdate", todoItem.Id, todoItem.Status);
        await _hubContext.Clients.All.SendAsync("ReceiveTodoCompletionDateUpdate", todoItem.Id, todoItem.DateCompleted);

        _logger.LogInformation("Todo item status toggled: {TodoId}", todoItem.Id);

        return Ok(new
        {
            TodoId = todoItem.Id,
            IsComplete = todoItem.IsComplete,
            Status = todoItem.Status,
            DateCompleted = todoItem.DateCompleted
        });
    }


    // DELETE: api/todo/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            try
            {
                var todoItem = await _context.TodoItems.FindAsync(id);
                if (todoItem == null)
                {
                    return NotFound();
                }
                _context.TodoItems.Remove(todoItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred in deletion: {ex.Message}");
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }
        
        [HttpPost("updateDueDate/{id}")]
        public async Task<IActionResult> UpdateDueDate(int id, [FromBody] DateTime newDueDate)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            todoItem.DueDate = newDueDate;

            // Reset status if due date is in the future
            if (todoItem.DueDate > DateTime.UtcNow && todoItem.Status == "Overdue")
            {
                todoItem.Status = "Pending";
            }

            _context.TodoItems.Update(todoItem);
            await _context.SaveChangesAsync();

            // Notify all clients about the due date change
            await _hubContext.Clients.All.SendAsync("ReceiveTodoDueDateUpdate", todoItem.Id, todoItem.DueDate);

            return Ok(new { todoItem.Id, todoItem.DueDate });
        }



        // PUT: api/todo/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodoItem(int id, [FromBody] TodoItem todoItemUpdate)
        {
            try
            {
                if (id != todoItemUpdate.Id)
                {
                    return BadRequest();
                }

                _context.Entry(todoItemUpdate).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TodoItems.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred in deletion: {ex.Message}");
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
            
        }
    }
}
