using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JobVault.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private const string ConfigKey = "Ingestion:ApiKey";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration.GetValue<string>(ConfigKey);

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Result = new ObjectResult("Ingestion API key is not configured on the server.")
                { StatusCode = StatusCodes.Status500InternalServerError };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || !string.Equals(expectedKey, providedKey.ToString(), StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult("Invalid or missing API key.");
            return;
        }

        await next();
    }
}
