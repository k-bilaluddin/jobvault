namespace JobVault.Infrastructure.Notifications;

internal sealed class Subscription(Action onDispose) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        onDispose();
    }
}
