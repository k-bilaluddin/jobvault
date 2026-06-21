using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.Extensions.Configuration;

namespace JobVault.Infrastructure.Auth;

public class AuthenticationService : IAuthenticationService
{
    private readonly string _ownerEmail;
    private readonly string _ownerPasswordHash;
    private readonly string _demoEmail;
    private readonly string _demoPasswordHash;
    private readonly ITokenService _tokenService;

    public AuthenticationService(IConfiguration configuration, ITokenService tokenService)
    {
        _ownerEmail = configuration["Auth:Email"] ?? "";
        _ownerPasswordHash = configuration["Auth:PasswordHash"] ?? "";
        _demoEmail = configuration["Demo:Email"] ?? "";
        _demoPasswordHash = configuration["Demo:PasswordHash"] ?? "";
        _tokenService = tokenService;
    }

    public AuthResponse? Login(string email, string password)
    {
        string? role = null;

        if (!string.IsNullOrEmpty(_ownerEmail) &&
            string.Equals(email, _ownerEmail, StringComparison.OrdinalIgnoreCase) &&
            BCrypt.Net.BCrypt.Verify(password, _ownerPasswordHash))
        {
            role = "owner";
        }
        else if (!string.IsNullOrEmpty(_demoEmail) &&
            string.Equals(email, _demoEmail, StringComparison.OrdinalIgnoreCase) &&
            BCrypt.Net.BCrypt.Verify(password, _demoPasswordHash))
        {
            role = "demo";
        }

        if (role is null) return null;

        var token = _tokenService.GenerateToken(email, role);
        return new AuthResponse { Token = token };
    }
}
