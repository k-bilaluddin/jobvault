using JobVault.Domain.Entities;
using System.Threading.Channels;

namespace JobVault.Application.Interfaces;

public interface INotificationHub
{
    (IDisposable Subscription, ChannelReader<AppNotification> Reader) Subscribe();
    Task BroadcastAsync(AppNotification notification);
}
