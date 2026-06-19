using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobVault.API.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var allowedEmail    = _configuration["Auth:Email"]        ?? string.Empty;
        var passwordHash    = _configuration["Auth:PasswordHash"] ?? string.Empty;
        var jwtSecret       = _configuration["Auth:JwtSecret"]    ?? string.Empty;
        var expiryDays      = int.TryParse(_configuration["Auth:TokenExpiryDays"], out var d) ? d : 7;

        var demoEmail       = _configuration["Demo:Email"]        ?? string.Empty;
        var demoPasswordHash = _configuration["Demo:PasswordHash"] ?? string.Empty;

        string? role = null;

        if (!string.IsNullOrEmpty(allowedEmail) &&
            string.Equals(request.Email, allowedEmail, StringComparison.OrdinalIgnoreCase) &&
            BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
        {
            role = "owner";
        }
        else if (!string.IsNullOrEmpty(demoEmail) &&
            string.Equals(request.Email, demoEmail, StringComparison.OrdinalIgnoreCase) &&
            BCrypt.Net.BCrypt.Verify(request.Password, demoPasswordHash))
        {
            role = "demo";
        }

        if (role is null)
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return Unauthorized(new { error = "Invalid credentials" });
        }

        var token = GenerateToken(request.Email, role, jwtSecret, expiryDays);
        _logger.LogInformation("Successful login for {Email} with role {Role}", request.Email, role);
        return Ok(new { token });
    }

    private static string GenerateToken(string email, string role, string secret, int expiryDays)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            claims:   claims,
            expires:  DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

