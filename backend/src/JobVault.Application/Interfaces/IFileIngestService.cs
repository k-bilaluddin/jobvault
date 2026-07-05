using JobVault.Application.Common;
using JobVault.Application.Models;

namespace JobVault.Application.Interfaces;

public interface IFileIngestService
{
    /// <summary>
    /// Commits files to the GitHub vault under "{companyName}-{applicationId}/". When
    /// <paramref name="applicationId"/> is null (the legacy synchronous upload path, which has
    /// no application record yet), falls back to the plain "{companyName}/" folder.
    /// </summary>
    Task<FileIngestResult> IngestAsync(
        string companyName,
        IReadOnlyCollection<IngestedFile> files,
        string? applicationId = null,
        CancellationToken cancellationToken = default);
}