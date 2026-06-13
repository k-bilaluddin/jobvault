using JobVault.API.Models.Requests;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IJobApplicationRepository _repository;
    private readonly ILogger<DashboardController> _logger;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public DashboardController(
        IJobApplicationRepository repository,
        ILogger<DashboardController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets dashboard statistics including total applications, status counts, and average score.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard statistics.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats(CancellationToken cancellationToken)
    {
        try
        {
            var (totalCount, statusCounts, averageScore) = await _repository.GetDashboardStatsAsync();

            var response = new DashboardStatsResponse
            {
                TotalApplications = totalCount,
                StatusCounts = statusCounts,
                AverageScore = Math.Round(averageScore, 2)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets recent activity with pagination.
    /// </summary>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated recent activity.</returns>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(PaginatedActivityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedActivityResponse>> GetActivity(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest(new { error = "Page must be greater than 0" });
            }

            if (pageSize < 1)
            {
                return BadRequest(new { error = "PageSize must be greater than 0" });
            }

            pageSize = Math.Min(pageSize, MaxPageSize);

            var (applications, totalCount) = await _repository.GetRecentActivityAsync(page, pageSize);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var activities = applications.Select(app => new ActivityResponse
            {
                Id = app.Id,
                CompanyName = app.CompanyName,
                JobTitle = app.JobTitle,
                ActivityType = DetermineActivityType(app),
                NewStatus = app.Status,
                Timestamp = app.UpdatedAt
            }).ToList();

            var response = new PaginatedActivityResponse
            {
                Items = activities,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard activity");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static string DetermineActivityType(Domain.Entities.JobApplication app)
    {
        var timeSinceCreation = app.UpdatedAt - app.CreatedAt;
        if (timeSinceCreation.TotalSeconds < 5)
        {
            return "Created";
        }
        return "Updated";
    }
}