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

    /// <summary>
    /// Gets a job application by ID.
    /// </summary>
    /// <param name="id">The application ID.</param>
    /// <returns>The job application or null if not found.</returns>
    Task<JobApplication?> GetApplicationByIdAsync(string id);

    /// <summary>
    /// Updates the status of a job application atomically.
    /// </summary>
    /// <param name="id">The application ID.</param>
    /// <param name="newStatus">The new status value.</param>
    /// <returns>The updated job application or null if not found.</returns>
    Task<JobApplication?> UpdateApplicationStatusAsync(string id, string newStatus);

    /// <summary>
    /// Gets dashboard statistics.
    /// </summary>
    /// <returns>Dashboard statistics including total count, status counts, and average score.</returns>
    Task<(int TotalCount, Dictionary<string, int> StatusCounts, double AverageScore)> GetDashboardStatsAsync();

    /// <summary>
    /// Gets recent activity with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A tuple containing the list of recent updates and total count.</returns>
    Task<(List<JobApplication> Applications, long TotalCount)> GetRecentActivityAsync(int page, int pageSize);
}