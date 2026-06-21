namespace JobVault.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(string email, string role);
    bool ValidateToken(string? token);
}
