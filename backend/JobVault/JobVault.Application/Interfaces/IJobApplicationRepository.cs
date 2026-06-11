using JobVault.Application.Common;
using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

/// <summary>
/// Repository interface for job application persistence operations.
/// </summary>
public interface IJobApplicationRepository
{
    /// <summary>
    /// Inserts or updates a job application in the repository.
    /// </summary>
    /// <param name="application">The job application to upsert.</param>
    /// <returns>The result of the upsert operation.</returns>
    Task<UpsertResult> UpsertApplicationAsync(JobApplication application);
}