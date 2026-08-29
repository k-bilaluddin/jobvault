using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace JobVault.API.Auth;

public static class AuthSchemes
{
    public const string ApiKey = "ApiKey";

    // Lets a controller action accept either the dashboard's JWT or a static API key
    // (e.g. the Claude Agent, or the LinkedIn capture extension) without the controller
    // itself needing to know anything about JWT handling.
    public const string JwtOrApiKey = $"{JwtBearerDefaults.AuthenticationScheme},{ApiKey}";
}
