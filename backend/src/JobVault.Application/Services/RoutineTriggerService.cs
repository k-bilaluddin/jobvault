using JobVault.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace JobVault.Application.Services;

public class RoutineTriggerService : IRoutineTriggerService
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromHours(24);

    private readonly IRoutineTriggerClient _client;
    private readonly IRoutineTriggerStateStore _stateStore;
    private readonly IPendingJobService _pendingJobs;
    private readonly ILogger<RoutineTriggerService> _logger;

    public RoutineTriggerService(
        IRoutineTriggerClient client,
        IRoutineTriggerStateStore stateStore,
        IPendingJobService pendingJobs,
        ILogger<RoutineTriggerService> logger)
    {
        _client = client;
        _stateStore = stateStore;
        _pendingJobs = pendingJobs;
        _logger = logger;
    }

    public async Task TriggerAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Manually triggering job-queue routine");
        await _client.RunAsync(ct);
        await _stateStore.SetLastTriggeredAtAsync(DateTime.UtcNow, ct);
    }

    public async Task<bool> TriggerIfDueAsync(CancellationToken ct = default)
    {
        var lastTriggeredAt = await _stateStore.GetLastTriggeredAtAsync(ct);
        if (lastTriggeredAt is not null && DateTime.UtcNow - lastTriggeredAt.Value < MinInterval)
        {
            _logger.LogDebug("Job-queue routine not due yet (last triggered {LastTriggeredAt:o})", lastTriggeredAt);
            return false;
        }

        var pending = await _pendingJobs.GetPendingAsync(ct);
        if (pending.Count == 0)
        {
            _logger.LogDebug("Job-queue routine due but no pending jobs — skipping");
            return false;
        }

        _logger.LogInformation("Auto-triggering job-queue routine for {Count} pending job(s)", pending.Count);
        await _client.RunAsync(ct);
        await _stateStore.SetLastTriggeredAtAsync(DateTime.UtcNow, ct);
        return true;
    }
}
