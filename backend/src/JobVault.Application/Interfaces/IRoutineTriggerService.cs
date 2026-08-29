namespace JobVault.Application.Interfaces;

public interface IRoutineTriggerService
{
    /// <summary>Fires the routine immediately, regardless of the 24h gate. Used by the dashboard's manual "Evaluate Jobs" button.</summary>
    Task TriggerAsync(CancellationToken ct = default);

    /// <summary>
    /// Fires the routine only if at least 24h have passed since the last trigger AND there are
    /// pending jobs in the queue. Used by the Worker's daily auto-trigger check.
    /// </summary>
    /// <returns>Whether the routine was actually fired.</returns>
    Task<bool> TriggerIfDueAsync(CancellationToken ct = default);
}
