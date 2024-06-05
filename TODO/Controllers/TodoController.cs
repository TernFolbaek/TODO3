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
using Microsoft.IdentityModel.Tokens;


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

        public TodoController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, ILogger<TodoController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        
        // GET: api/todo
        [HttpGet]
        public async Task<IActionResult> GetAllTodoItems()
        {
            if (!TryValidateToken(out ClaimsPrincipal validatedPrincipal))
            {
                _logger.LogInformation("invalid token");
                return Unauthorized("Invalid JWT token.");
            }

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
            // Validate JWT token
            if (!TryValidateToken(out ClaimsPrincipal validatedPrincipal))
            {
                _logger.LogInformation("invalid token");
                return Unauthorized("Invalid JWT token.");
            }
            try
            {
                _logger.LogInformation("valid token");
                // Check if all provided usernames exist in the database
                var users = await _context.Users
                    .Where(u => todoItemRequest.Usernames.Contains(u.Username))
                    .ToListAsync();

                if (users.Count != todoItemRequest.Usernames.Count)
                {
                    return BadRequest("One or more usernames do not exist.");
                }

                var todoItem = new TodoItem
                {
                    Id = todoItemRequest.Id, // Consider letting the database handle the Id generation unless there's a specific reason for setting it manually
                    IsComplete = todoItemRequest.IsComplete,
                    Description = todoItemRequest.Description,
                    DueDate = todoItemRequest.DueDate,
                    UserTodos = users.Select(user => new UserTodo { User = user }).ToList()
                };

                _context.TodoItems.Add(todoItem);
                await _context.SaveChangesAsync();
                return Ok(new {
                    TodoId = todoItem.Id,
                    Description = todoItem.Description,
                    IsComplete = todoItem.IsComplete,
                    DueDate = todoItem.DueDate,
                    UserTodos = todoItem.UserTodos.Select(ut => new { ut.User.Id, ut.User.Username }).ToList() // Simplify for serialization
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating TODO item: {ex.Message}", ex);
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
            
        }
        
        private bool TryValidateToken(out ClaimsPrincipal validatedPrincipal)
        {
            validatedPrincipal = null;

            var authorizationHeader = Request.Headers["Authorization"];
            if (authorizationHeader.FirstOrDefault() == null || !authorizationHeader.FirstOrDefault().StartsWith("Bearer "))
            {
                return false;
            }

            var tokenString = authorizationHeader.FirstOrDefault().Substring("Bearer ".Length);

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JWTSecureKey").Value)),
                    
                    ValidateIssuer = true,
                    ValidIssuer = "YourIssuer",
                    ValidateAudience = true,
                    ValidAudience = "YourAudience",
                    ValidateLifetime = true
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                validatedPrincipal = tokenHandler.ValidateToken(tokenString, tokenValidationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch (SecurityTokenException e)
            {
                return false;
            }
        }


       [HttpGet("{id}")]
public async Task<IActionResult> GetTodoItem(int id, [FromQuery] string timezone = "UTC")
{
    // Validate JWT token
    if (!TryValidateToken(out ClaimsPrincipal validatedPrincipal))
    {
        _logger.LogInformation("invalid token");
        return Unauthorized("Invalid JWT token.");
    }

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
                .Include(ti => ti.UserTodos) // Include UserTodos
                    .ThenInclude(ut => ut.User) // Then include User data
                .FirstOrDefaultAsync(ti => ti.Id == id); // Find the TodoItem by id

            if (todoItem == null)
            {
                return NotFound("Todo item not found.");
            }

            if (todoItem.DueDate.HasValue)
            {
                var localDateTimeOffset = todoItem.DueDate.Value.ToOffset(utcOffset);
                _logger.LogInformation($"Adjusted DueDate: {todoItem.DueDate}");
                todoItem.DueDate = localDateTimeOffset; // Update the DueDate with the adjusted local time
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
                }).ToList() // Projecting necessary user data
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



        
    
        
        // DELETE: api/todo/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            if (!TryValidateToken(out ClaimsPrincipal validatedPrincipal))
            {
                _logger.LogInformation("invalid token");
                return Unauthorized("Invalid JWT token.");
            }

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

        // PUT: api/todo/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodoItem(int id, [FromBody] TodoItem todoItemUpdate)
        {
            if (!TryValidateToken(out ClaimsPrincipal validatedPrincipal))
            {
                _logger.LogInformation("invalid token");
                return Unauthorized("Invalid JWT token.");
            }

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
