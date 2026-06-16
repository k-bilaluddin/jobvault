using System.Threading.Channels;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Notifications;

public class NotificationHub : INotificationHub
{
    private readonly List<Channel<AppNotification>> _clients = [];
    private readonly Lock _lock = new();
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public (IDisposable Subscription, ChannelReader<AppNotification> Reader) Subscribe()
    {
        var channel = Channel.CreateUnbounded<AppNotification>();
        lock (_lock)
        {
            _clients.Add(channel);
        }
        _logger.LogInformation("SSE client subscribed. Total clients: {Count}", _clients.Count);

        var subscription = new Subscription(() =>
        {
            lock (_lock)
            {
                _clients.Remove(channel);
                channel.Writer.TryComplete();
            }
            _logger.LogInformation("SSE client disconnected. Total clients: {Count}", _clients.Count);
        });

        return (subscription, channel.Reader);
    }

    public async Task BroadcastAsync(AppNotification notification)
    {
        List<Channel<AppNotification>> snapshot;
        lock (_lock)
        {
            snapshot = [.._clients];
        }

        foreach (var client in snapshot)
        {
            await client.Writer.WriteAsync(notification);
        }

        _logger.LogInformation("Broadcast notification to {Count} SSE clients", snapshot.Count);
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            onDispose();
        }
    }
}
