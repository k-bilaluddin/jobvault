using JobVault.API.Auth;
using JobVault.Contracts.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JobVault.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var result = ApiKeyValidator.Validate(context.HttpContext.Request.Headers, configuration);

        if (result == ApiKeyValidationResult.NotConfigured)
        {
            var problem = ErrorCatalog.ToProblem("auth.api_key_not_configured", context.HttpContext);
            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            return;
        }

        if (result != ApiKeyValidationResult.Valid)
        {
            var problem = ErrorCatalog.ToProblem("auth.api_key_missing", context.HttpContext);
            context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
            return;
        }

        await next();
    }
}
