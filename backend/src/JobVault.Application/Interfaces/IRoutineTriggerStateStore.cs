namespace JobVault.Application.Interfaces;

/// <summary>
/// Persists when the job-queue routine was last fired, so the 24h auto-trigger gate survives
/// Worker restarts.
/// </summary>
public interface IRoutineTriggerStateStore
{
    Task<DateTime?> GetLastTriggeredAtAsync(CancellationToken ct = default);
    Task SetLastTriggeredAtAsync(DateTime triggeredAtUtc, CancellationToken ct = default);
}
