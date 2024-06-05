using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TODO.Data;
using TODO.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions.Common;


namespace TODO.Controllers
{
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;  

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration; 

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
                var token = GenerateJwtToken(user);
                return Ok(new { token = token, message = "Login successful" });
            }

            return BadRequest(new { error = "Invalid login attempt." });
        }

        private string GenerateJwtToken(User user)
        {

            var jwtKey = _configuration.GetSection("JWTSecureKey").Value; // Make sure the key name matches what's in appsettings.json

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: "YourIssuer",
                audience: "YourAudience",
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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

            // Generate token right after saving the new user
            var token = GenerateJwtToken(newUser);

            return Ok(new { token = token, message = "Signup successful" });
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
