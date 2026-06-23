using JobVault.Contracts.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is AppException appEx)
        {
            var error = ErrorCatalog.Get(appEx.ErrorCode);
            problem = error.ToProblemDetails(httpContext, appEx.Args);
            _logger.LogWarning(exception, "Application error {ErrorCode}", appEx.ErrorCode);
        }
        else
        {
            var error = ErrorCatalog.Get("server.internal_error");
            problem = error.ToProblemDetails(httpContext);
            _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
