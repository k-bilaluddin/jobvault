using JobVault.Contracts.Responses;

namespace JobVault.Application.Interfaces;

public interface IAuthenticationService
{
    AuthResponse? Login(string email, string password);
}
