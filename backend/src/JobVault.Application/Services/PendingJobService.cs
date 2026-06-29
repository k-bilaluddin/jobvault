using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.Application.Services;

public class PendingJobService : IPendingJobService
{
    private readonly IPendingJobRepository _repository;
    private readonly ILogger<PendingJobService> _logger;

    public PendingJobService(IPendingJobRepository repository, ILogger<PendingJobService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PendingJob> CreateAsync(string url, string? prompt = null, CancellationToken ct = default)
    {
        var job = await _repository.CreateAsync(url, prompt, ct);
        _logger.LogInformation("Queued pending job {JobId} for {Url}", job.Id, url);
        return job;
    }

    public Task<IReadOnlyList<PendingJob>> GetAllAsync(string? status = null, CancellationToken ct = default)
        => _repository.GetAllAsync(status, ct);

    public Task<IReadOnlyList<PendingJob>> GetPendingAsync(CancellationToken ct = default)
        => _repository.GetPendingAsync(ct);

    public Task<bool> UpdateAsync(string id, string? url, string? status, CancellationToken ct = default)
        => _repository.UpdateAsync(id, url, status, ct);

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
        => _repository.DeleteAsync(id, ct);

    public Task<int> CleanupByStatusAsync(string status, CancellationToken ct = default)
        => _repository.DeleteByStatusAsync(status, ct);
}
