using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;

namespace JobVault.Application.Services;

public class NotificationQueryService : INotificationQueryService
{
    private readonly INotificationRepository _repository;

    public NotificationQueryService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetRecentAsync(count);
        return notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            Type = n.Type,
            Title = n.Title,
            Body = n.Body,
            CompanyName = n.CompanyName,
            CompanySlug = n.CompanySlug,
            OccurredAt = n.OccurredAt.ToString("o"),
            Read = n.Read,
        }).ToList();
    }

    public Task<long> MarkAllReadAsync(CancellationToken cancellationToken = default)
        => _repository.MarkAllReadAsync();

    public Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.MarkReadAsync(id);
}
