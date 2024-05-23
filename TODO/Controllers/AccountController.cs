using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TODO.Data;
using TODO.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace TODO.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);
            if (user != null)
            {
                HttpContext.Session.SetString("LoggedInUser", user.Username);
                return Ok(new { message = "Login successful" });
            }
    
            return BadRequest(new { error = "Invalid login attempt." });
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Username == model.Username);
            if (existingUser)
            {
                return BadRequest(new { error = "Username already exists." });
            }

            var newUser = new User { Username = model.Username, Password = model.Password };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
    
            HttpContext.Session.SetString("LoggedInUser", newUser.Username);

            // Enqueue a simple background job to log a message
            BackgroundJob.Enqueue(() => LogNewUserSignup(newUser.Username));

            return Ok(new { message = "Signup successful" });
        }

        public void LogNewUserSignup(string username)
        {
            _logger.LogInformation($"New user signed up: {username}");

        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("LoggedInUser");
            return Ok(new { message = "Logged out successfully" });
        }
    }
}
