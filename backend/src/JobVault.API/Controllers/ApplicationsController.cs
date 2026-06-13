using JobVault.API.Models.Requests;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IJobApplicationRepository _repository;
    private readonly ILogger<ApplicationsController> _logger;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public ApplicationsController(
        IJobApplicationRepository repository,
        ILogger<ApplicationsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated job applications with optional filtering.
    /// </summary>
    /// <param name="request">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of job applications.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedApplicationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedApplicationsResponse>> GetApplications(
        [FromQuery] GetApplicationsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate pagination parameters
            if (request.Page < 1)
            {
                return BadRequest(new { error = "Page must be greater than 0" });
            }

            if (request.PageSize < 1)
            {
                return BadRequest(new { error = "PageSize must be greater than 0" });
            }

            // Apply default and max page size
            var pageSize = request.PageSize == 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);

            // Validate score range
            if (request.MinScore.HasValue && request.MaxScore.HasValue && request.MinScore > request.MaxScore)
            {
                return BadRequest(new { error = "MinScore cannot be greater than MaxScore" });
            }

            // Validate date range
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
            {
                return BadRequest(new { error = "FromDate cannot be after ToDate" });
            }

            _logger.LogInformation(
                "Fetching applications - Page: {Page}, PageSize: {PageSize}, Status: {Status}, Company: {Company}",
                request.Page,
                pageSize,
                request.Status ?? "all",
                request.Company ?? "all");

            var (applications, totalCount) = await _repository.GetApplicationsAsync(
                request.Page,
                pageSize,
                request.Status,
                request.Company,
                request.FromDate,
                request.ToDate,
                request.MinScore,
                request.MaxScore);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new PaginatedApplicationsResponse
            {
                Items = applications.Select(app => new ApplicationResponse
                {
                    Id = app.Id,
                    CompanyName = app.CompanyName,
                    JobTitle = app.JobTitle,
                    Location = app.Location,
                    JobUrl = app.JobUrl,
                    WorkMode = app.WorkMode,
                    EmploymentType = app.EmploymentType,
                    SalaryMin = app.SalaryMin,
                    SalaryMax = app.SalaryMax,
                    Currency = app.Currency,
                    SalaryPeriod = app.SalaryPeriod,
                    MatchScore = app.MatchScore,
                    Recommendation = app.Recommendation,
                    Status = app.Status,
                    CreatedAt = app.CreatedAt,
                    UpdatedAt = app.UpdatedAt
                }).ToList(),
                TotalCount = (int)totalCount,
                Page = request.Page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving applications");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}