using System.Diagnostics;

namespace JobVault.Infrastructure.Generation;

/// <summary>
/// Propagates the active W3C trace context (traceparent / tracestate) to outgoing HTTP requests
/// so the generation service can link its spans back to the Worker's trace.
/// </summary>
public sealed class TracePropagationHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            // W3C format: 00-<traceId>-<spanId>-<flags>
            request.Headers.TryAddWithoutValidation("traceparent", activity.Id);

            if (!string.IsNullOrEmpty(activity.TraceStateString))
                request.Headers.TryAddWithoutValidation("tracestate", activity.TraceStateString);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
