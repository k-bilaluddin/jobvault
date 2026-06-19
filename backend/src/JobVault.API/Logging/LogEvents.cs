using Microsoft.Extensions.Logging;

namespace JobVault.API.Logging;

internal static class LogEvents
{
    // SSE / Notifications — 1001–1009
    internal static readonly EventId SseClientConnected      = new(1001, nameof(SseClientConnected));
    internal static readonly EventId SseClientDisconnected   = new(1002, nameof(SseClientDisconnected));
    internal static readonly EventId SseStreamError          = new(1003, nameof(SseStreamError));
    internal static readonly EventId SseClientCancelled      = new(1004, nameof(SseClientCancelled));

    // Application ingestion — 1010–1019
    internal static readonly EventId ApplicationIngested     = new(1010, nameof(ApplicationIngested));
    internal static readonly EventId ApplicationIngestFailed = new(1011, nameof(ApplicationIngestFailed));

    // Notification read state — 1020–1029
    internal static readonly EventId NotificationsMarkedRead = new(1020, nameof(NotificationsMarkedRead));
    internal static readonly EventId NotificationMarkedRead  = new(1021, nameof(NotificationMarkedRead));
}
