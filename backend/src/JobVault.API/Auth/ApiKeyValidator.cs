using Microsoft.AspNetCore.Http;

namespace JobVault.API.Auth;

public enum ApiKeyValidationResult { Valid, Missing, Invalid, NotConfigured }

// Shared by ApiKeyAttribute (action-filter auth for anonymous endpoints like GET /pending)
// and ApiKeyAuthenticationHandler (a real auth scheme, combinable with JWT via [Authorize(AuthenticationSchemes = ...)]).
public static class ApiKeyValidator
{
    public const string HeaderName = "X-Api-Key";
    public const string ConfigKey = "Ingestion:ApiKey";

    public static ApiKeyValidationResult Validate(IHeaderDictionary headers, IConfiguration configuration)
    {
        var expectedKey = configuration.GetValue<string>(ConfigKey);
        if (string.IsNullOrWhiteSpace(expectedKey))
            return ApiKeyValidationResult.NotConfigured;

        if (!headers.TryGetValue(HeaderName, out var providedKey))
            return ApiKeyValidationResult.Missing;

        return string.Equals(expectedKey, providedKey.ToString(), StringComparison.Ordinal)
            ? ApiKeyValidationResult.Valid
            : ApiKeyValidationResult.Invalid;
    }
}
