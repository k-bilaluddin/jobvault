using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace JobVault.API.Auth;

// Registers the static ingestion API key as a real authentication scheme (name: AuthSchemes.ApiKey)
// so it can be combined with the JWT scheme via [Authorize(AuthenticationSchemes = AuthSchemes.JwtOrApiKey)].
// Stacking [Authorize] with the ApiKeyAttribute action filter would NOT work as either/or:
// AuthorizeAttribute runs in the authorization-filter stage and would 401 before an action
// filter ever executes. A second real scheme is the correct fix.
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = ApiKeyValidator.Validate(Request.Headers, _configuration);

        return Task.FromResult(result switch
        {
            ApiKeyValidationResult.Valid => AuthenticateResult.Success(BuildTicket()),
            // No header at all isn't a failure of this scheme — let the JWT scheme decide.
            ApiKeyValidationResult.Missing => AuthenticateResult.NoResult(),
            ApiKeyValidationResult.NotConfigured => AuthenticateResult.Fail("Ingestion API key is not configured."),
            _ => AuthenticateResult.Fail("Invalid API key."),
        });
    }

    private AuthenticationTicket BuildTicket()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "extension-client")], Scheme.Name);
        return new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
    }
}
