using JobVault.API.Models.Requests;
using JobVault.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var result = _authService.Login(request.Email, request.Password);

        if (result is null)
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return ErrorResponse("auth.invalid_credentials");
        }

        _logger.LogInformation("Successful login for {Email}", request.Email);
        return Ok(result);
    }
}
