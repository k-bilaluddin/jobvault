using JobVault.Contracts.Errors;
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
            var problem = ErrorCatalog.ToProblem("auth.api_key_not_configured", context.HttpContext);
            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || !string.Equals(expectedKey, providedKey.ToString(), StringComparison.Ordinal))
        {
            var problem = ErrorCatalog.ToProblem("auth.api_key_missing", context.HttpContext);
            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            return;
        }

        await next();
    }
}
