using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

public interface INotificationRepository
{
    Task SaveAsync(AppNotification notification);
    Task<IEnumerable<AppNotification>> GetRecentAsync(int count = 50);
    Task MarkAllReadAsync();
    Task MarkReadAsync(Guid id);
}
