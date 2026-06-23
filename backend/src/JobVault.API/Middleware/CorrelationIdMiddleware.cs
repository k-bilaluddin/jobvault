namespace JobVault.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Trace-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming) &&
            !string.IsNullOrWhiteSpace(incoming))
        {
            context.TraceIdentifier = incoming!;
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        using (context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("JobVault.API")
            .BeginScope(new Dictionary<string, object> { ["TraceId"] = context.TraceIdentifier }))
        {
            await _next(context);
        }
    }
}
