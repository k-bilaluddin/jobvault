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

    /// <summary>
    /// Gets paginated job applications with optional filtering.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="company">Optional company name filter (case-insensitive partial match).</param>
    /// <param name="fromDate">Optional start date filter (inclusive).</param>
    /// <param name="toDate">Optional end date filter (inclusive).</param>
    /// <param name="minScore">Optional minimum match score filter.</param>
    /// <param name="maxScore">Optional maximum match score filter.</param>
    /// <returns>A tuple containing the list of applications and total count.</returns>
    Task<(List<JobApplication> Applications, long TotalCount)> GetApplicationsAsync(
        int page,
        int pageSize,
        string? status = null,
        string? company = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? minScore = null,
        int? maxScore = null);
}