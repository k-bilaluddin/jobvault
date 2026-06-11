using JobVault.Application.Common;
using JobVault.Application.Models;

namespace JobVault.Application.Interfaces;

public interface IFileIngestService
{
    Task<FileIngestResult> IngestAsync(
        string companyName,
        IReadOnlyCollection<IngestedFile> files,
        CancellationToken cancellationToken = default);
}