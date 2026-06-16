using JobVault.Application.Common;
using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

/// <summary>
/// Repository interface for job application persistence operations.
/// </summary>
public interface IJobApplicationRepository
{
    /// <summary>
    /// Inserts or updates a job application. Returns the MongoDB ObjectId in UpsertResult.Id.
    /// </summary>
    Task<UpsertResult> UpsertApplicationAsync(JobApplication application);

    /// <summary>
    /// Fetches a job application by its MongoDB ObjectId string.
    /// </summary>
    Task<JobApplication?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates status and optional fields on an existing document. Clears blob fields on terminal states.
    /// </summary>
    Task<bool> UpdateStatusAsync(
        string id,
        string status,
        string? commitUrl = null,
        string? errorDetails = null,
        CancellationToken cancellationToken = default);
}