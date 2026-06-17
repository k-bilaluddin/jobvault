namespace JobVault.Application.Interfaces;

public interface IApplicationProcessorService
{
    Task ProcessAsync(string applicationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called by the consumer after all retries are exhausted to permanently record the failure
    /// and emit the failure event — keeps the processor as the sole owner of application state transitions.
    /// </summary>
    Task MarkFailedAsync(string applicationId, string reason, CancellationToken cancellationToken = default);
}
