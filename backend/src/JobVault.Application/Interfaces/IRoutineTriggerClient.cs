namespace JobVault.Application.Interfaces;

/// <summary>
/// Fires the external Claude routine that evaluates pending job-queue entries and submits
/// applications via the ingestion API. Implemented in Infrastructure as a thin wrapper around
/// the claude.ai remote-trigger HTTP API.
/// </summary>
public interface IRoutineTriggerClient
{
    Task RunAsync(CancellationToken ct = default);
}
