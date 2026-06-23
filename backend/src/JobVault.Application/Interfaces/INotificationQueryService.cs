using JobVault.Contracts.Responses;

namespace JobVault.Application.Interfaces;

public interface INotificationQueryService
{
    Task<IReadOnlyList<NotificationResponse>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
    Task<long> MarkAllReadAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default);
}
