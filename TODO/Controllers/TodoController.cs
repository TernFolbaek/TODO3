using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TODO.Data;
using TODO.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TODO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TodoController> _logger;

        public TodoController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<TodoController> logger)
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // GET: api/todo
        [HttpGet]
        public async Task<IActionResult> GetAllTodoItems()
        {
            var todoItems = await _context.TodoItems.ToListAsync();
            return Ok(todoItems);
        }


        public record TodoItemRequest(int Id, string Description, DateTime? DueDate, bool IsComplete);
        // POST: api/todo
        [HttpPost]
        public async Task<IActionResult> CreateTodoItem([FromBody] TodoItemRequest todoItemRequest)
        {
            try
            {
                var todoItem = new TodoItem
                {
                    Id = todoItemRequest.Id,
                    IsComplete = todoItemRequest.IsComplete,
                    Description = todoItemRequest.Description,
                    DueDate = todoItemRequest.DueDate
                };
                _context.TodoItems.Add(todoItem);
                await _context.SaveChangesAsync();
                return Ok(todoItem);
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
            using (var client = _httpClientFactory.CreateClient())
            {
                try
                {
                    var response = await client.GetAsync($"http://worldtimeapi.org/api/timezone/{timezone}");
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest("Failed to retrieve timezone information from WorldTimeApi.");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);
                    var utcOffset = ParseUtcOffset(json["utc_offset"].ToString());

                    var todoItem = await _context.TodoItems.FindAsync(id);
                    if (todoItem == null)
                    {
                        return NotFound("Todo item not found.");
                    }

                    if (todoItem.DueDate.HasValue)
                    {
                        _logger.LogInformation($"Original UTC DueDate: {todoItem.DueDate}");
                        var localDateTimeOffset = todoItem.DueDate.Value.ToOffset(utcOffset);
                        _logger.LogInformation($"Local DueDate after applying offset: {localDateTimeOffset}");

                        // Update the DueDate with the adjusted local time
                        todoItem.DueDate = localDateTimeOffset;
                        _logger.LogInformation($"YEEHAW new date for the todo: {todoItem.DueDate}");
                    }

                    // Return the todo item with the adjusted DueDate
                    return Ok(todoItem);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred: {ex.Message}");
                    return StatusCode(500, "Internal Server Error: " + ex.Message);
                }
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
                    throw new FormatException("Invalid time span format.");
                }
            }
            else
            {
                _logger.LogError($"Unexpected UTC offset format: {utcOffset}");
                throw new FormatException("Unexpected time span format, expected a leading '+' or '-'");
            }
        }



        
    
        
        // DELETE: api/todo/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
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

        // PUT: api/todo/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodoItem(int id, [FromBody] TodoItem todoItemUpdate)
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
    }
}
