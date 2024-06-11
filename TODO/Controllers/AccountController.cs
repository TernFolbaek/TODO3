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
using Npgsql.Replication.PgOutput.Messages;

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

        var cookiesOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(7),
            Secure = true,
            SameSite = SameSiteMode.Strict // or None if needed
        };

        Response.Cookies.Append("AccessToken", accessToken, cookiesOptions);
        Response.Cookies.Append("RefreshToken", refreshToken, cookiesOptions);

        return Ok(new { message = "Login successful" });
    }

    _logger.LogWarning($"Login failed for user {model.Username}: Invalid username or password");
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

    var hashedPassword = HashPassword(model.Password);
    var newUser = new User
    {
        Username = model.Username,
        Password = hashedPassword,
        RefreshToken = GenerateRefreshToken(),
        RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
    };

    _context.Users.Add(newUser);
    await _context.SaveChangesAsync();

    var (accessToken, refreshToken) = GenerateTokens(newUser);

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Expires = DateTime.UtcNow.AddDays(7),
        Secure = true,
        SameSite = SameSiteMode.Strict 
    };

    Response.Cookies.Append("AccessToken", accessToken, cookieOptions);
    Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);

    return Ok(new { message = "Signup successful" });
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
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddYears(-1), 
            };

            Response.Cookies.Delete("AccessToken", cookieOptions);
            Response.Cookies.Delete("RefreshToken", cookieOptions);

            return Ok(new { message = "Logged out successfully" });
        }


        
        [HttpGet("check-session")]
        public IActionResult CheckSession()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Ok(new { message = "Session is active." });
            }
            else
            {
                // If the JWT is invalid or expired
                return Unauthorized(new { message = "Session expired or invalid." });
            }
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
