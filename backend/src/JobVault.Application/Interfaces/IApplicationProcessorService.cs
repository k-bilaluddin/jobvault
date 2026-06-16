namespace JobVault.Application.Interfaces;

public interface IApplicationProcessorService
{
    Task ProcessAsync(string applicationId, CancellationToken cancellationToken = default);
}
