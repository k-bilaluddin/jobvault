using JobVault.API.Models.Requests;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IJobApplicationRepository _repository;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<ApplicationsController> _logger;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public ApplicationsController(
        IJobApplicationRepository repository,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<ApplicationsController> logger)
    {
        _repository = repository;
        _rabbitMqPublisher = rabbitMqPublisher;
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

    /// <summary>
    /// Gets a single job application by ID.
    /// </summary>
    /// <param name="id">The application ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full application details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApplicationResponse>> GetApplicationById(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var application = await _repository.GetApplicationByIdAsync(id);

            if (application == null)
            {
                return NotFound(new { error = $"Application with ID '{id}' not found" });
            }

            var response = new ApplicationResponse
            {
                Id = application.Id,
                CompanyName = application.CompanyName,
                JobTitle = application.JobTitle,
                Location = application.Location,
                JobUrl = application.JobUrl,
                WorkMode = application.WorkMode,
                EmploymentType = application.EmploymentType,
                SalaryMin = application.SalaryMin,
                SalaryMax = application.SalaryMax,
                Currency = application.Currency,
                SalaryPeriod = application.SalaryPeriod,
                MatchScore = application.MatchScore,
                Recommendation = application.Recommendation,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application by ID: {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates the status of a job application.
    /// </summary>
    /// <param name="id">The application ID.</param>
    /// <param name="request">The new status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated application.</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatus(
        string id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { error = "Status is required" });
            }

            var updatedApplication = await _repository.UpdateApplicationStatusAsync(id, request.Status);

            if (updatedApplication == null)
            {
                return NotFound(new { error = $"Application with ID '{id}' not found" });
            }

            // Emit event to RabbitMQ
            var statusChangeEvent = new JobApplicationEvent
            {
                CompanyName = updatedApplication.CompanyName,
                JobTitle = updatedApplication.JobTitle,
                MatchScore = updatedApplication.MatchScore,
                Recommendation = updatedApplication.Recommendation,
                Status = updatedApplication.Status,
                URL = updatedApplication.JobUrl,
                EventType = "StatusChanged",
                Timestamp = DateTime.UtcNow
            };

            await _rabbitMqPublisher.PublishJobApplicationEventAsync(statusChangeEvent);

            var response = new ApplicationResponse
            {
                Id = updatedApplication.Id,
                CompanyName = updatedApplication.CompanyName,
                JobTitle = updatedApplication.JobTitle,
                Location = updatedApplication.Location,
                JobUrl = updatedApplication.JobUrl,
                WorkMode = updatedApplication.WorkMode,
                EmploymentType = updatedApplication.EmploymentType,
                SalaryMin = updatedApplication.SalaryMin,
                SalaryMax = updatedApplication.SalaryMax,
                Currency = updatedApplication.Currency,
                SalaryPeriod = updatedApplication.SalaryPeriod,
                MatchScore = updatedApplication.MatchScore,
                Recommendation = updatedApplication.Recommendation,
                Status = updatedApplication.Status,
                CreatedAt = updatedApplication.CreatedAt,
                UpdatedAt = updatedApplication.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for application {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets files associated with a job application.
    /// </summary>
    /// <param name="id">The application ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file metadata.</returns>
    [HttpGet("{id}/files")]
    [ProducesResponseType(typeof(List<FileMetadataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<FileMetadataResponse>>> GetApplicationFiles(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var application = await _repository.GetApplicationByIdAsync(id);

            if (application == null)
            {
                return NotFound(new { error = $"Application with ID '{id}' not found" });
            }

            // Build file metadata list
            // Assuming files are stored in a pattern: {companyName}/{filename}
            var files = new List<FileMetadataResponse>
            {
                new FileMetadataResponse
                {
                    Name = $"{application.CompanyName}_job_description.md",
                    Size = 0, // Will be determined when file is fetched
                    ContentType = "text/markdown",
                    Url = $"/api/files/{id}/{application.CompanyName}_job_description.md"
                },
                new FileMetadataResponse
                {
                    Name = $"{application.CompanyName}_analysis.md",
                    Size = 0,
                    ContentType = "text/markdown",
                    Url = $"/api/files/{id}/{application.CompanyName}_analysis.md"
                }
            };

            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files for application {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}