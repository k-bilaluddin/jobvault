using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

public interface IDocumentGenerationClient
{
    Task<byte[]> GenerateCvAsync(JobApplication application, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCoverLetterAsync(JobApplication application, CancellationToken cancellationToken = default);
}
