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
using System.Security.Cryptography;
using System.Text;
using FluentAssertions.Common;
namespace TODO.Models
{
    public class TokenRequest
    {
        public string RefreshToken { get; set; }
    }
}


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


       
        private (string AccessToken, string RefreshToken) GenerateTokens(User user)
        {
            try
            {
                var jwtKey = _configuration["JWTSecureKey"];
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                var accessToken = new JwtSecurityToken(
                    issuer: "YourIssuer",
                    audience: "YourAudience",
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(10),
                    signingCredentials: credentials
                );

                var writtenToken = new JwtSecurityTokenHandler().WriteToken(accessToken);
                var refreshToken = GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); 
                _context.Users.Update(user);
                _context.SaveChanges();

                return (writtenToken, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating tokens: {ex}");
                throw;
            }
        }

        

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hashedPassword = HashPassword(model.Password);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == hashedPassword);

            if (user != null)
            {
                var (accessToken, refreshToken) = GenerateTokens(user);
                return Ok(new { accessToken, refreshToken, message = "Login successful" });
            }

            _logger.LogWarning($"Login failed for user {model.Username}: Invalid username or password");
            return BadRequest(new { error = "Invalid login attempt." });
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
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

            var hashedPassword = HashPassword(model.Password);
            var newUser = new User 
            { 
                Username = model.Username, 
                Password = hashedPassword,
                RefreshToken = GenerateRefreshToken(),
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _context.Users.Add(newUser);
            try
            {
                await _context.SaveChangesAsync();
                var (accessToken, refreshToken) = GenerateTokens(newUser);
                return Ok(new { accessToken, refreshToken, message = "Signup successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during signup: {ex}");
                return StatusCode(500, "An error occurred while creating the user account.");
            }
        }



        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest request)
        {
            _logger.LogInformation($"Received refresh token request with token: {request.RefreshToken}");
            var utcNow = DateTime.UtcNow; 
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken 
                                          && u.RefreshTokenExpiryTime > utcNow);
            if (user == null)
            {
                _logger.LogWarning("Invalid refresh token or token expired.");
                return Unauthorized("Invalid refresh token.");
            }

            var newTokens = GenerateTokens(user); 
            _logger.LogInformation($"Generated new access token: {newTokens.AccessToken}");
            _logger.LogInformation($"Generated new refresh token: {newTokens.RefreshToken}");
            return Ok(new { accessToken = newTokens.AccessToken, refreshToken = newTokens.RefreshToken });
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
        
        // GET: api/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.Select(u => new { u.Id, u.Username }).ToListAsync();
            return Ok(users);
        }

    }
}
