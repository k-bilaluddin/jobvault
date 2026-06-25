using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

public interface IPendingJobService
{
    Task<PendingJob> CreateAsync(string url, CancellationToken ct = default);
    Task<IReadOnlyList<PendingJob>> GetAllAsync(string? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<PendingJob>> GetPendingAsync(CancellationToken ct = default);
    Task<bool> UpdateAsync(string id, string? url, string? status, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    Task<int> CleanupByStatusAsync(string status, CancellationToken ct = default);
}
