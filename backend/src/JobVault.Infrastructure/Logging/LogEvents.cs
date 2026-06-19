using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Logging;

internal static class LogEvents
{
    // RabbitMQ publisher — 2001–2009
    internal static readonly EventId RabbitMqInitialized    = new(2001, nameof(RabbitMqInitialized));
    internal static readonly EventId RabbitMqInitFailed     = new(2002, nameof(RabbitMqInitFailed));
    internal static readonly EventId RabbitMqPublished      = new(2003, nameof(RabbitMqPublished));
    internal static readonly EventId RabbitMqPublishFailed  = new(2004, nameof(RabbitMqPublishFailed));

    // Application processing — 2010–2019
    internal static readonly EventId ProcessingStarted      = new(2010, nameof(ProcessingStarted));
    internal static readonly EventId ProcessingSucceeded    = new(2011, nameof(ProcessingSucceeded));
    internal static readonly EventId ProcessingFailed       = new(2012, nameof(ProcessingFailed));
    internal static readonly EventId DirectoryCleanupFailed = new(2013, nameof(DirectoryCleanupFailed));

    // Notification persistence — 2020–2029
    internal static readonly EventId NotificationSaved      = new(2020, nameof(NotificationSaved));
    internal static readonly EventId NotificationSaveFailed = new(2021, nameof(NotificationSaveFailed));
    internal static readonly EventId NotificationsRead      = new(2022, nameof(NotificationsRead));
    internal static readonly EventId NotificationReadFailed = new(2023, nameof(NotificationReadFailed));

    // SSE hub — 2030–2039
    internal static readonly EventId SseSubscribed          = new(2030, nameof(SseSubscribed));
    internal static readonly EventId SseUnsubscribed        = new(2031, nameof(SseUnsubscribed));
    internal static readonly EventId SseBroadcast           = new(2032, nameof(SseBroadcast));
}
