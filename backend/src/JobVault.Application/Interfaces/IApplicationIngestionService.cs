using JobVault.Application.Common;
using JobVault.Contracts.Requests;

namespace JobVault.Application.Interfaces;

public interface IApplicationIngestionService
{
    Task<ApplicationIngestionResult> IngestAsync(
        IngestApplicationRequest request,
        CancellationToken cancellationToken = default);
}
