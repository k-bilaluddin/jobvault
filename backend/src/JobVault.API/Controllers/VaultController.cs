using JobVault.API.Models.Requests;
using JobVault.Application.Interfaces;
using JobVault.Application.Models;
using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api")]
public class VaultController : ControllerBase
{
    private readonly IFileIngestService _fileIngestService;
    private readonly IApplicationIngestionService _applicationIngestionService;
    private readonly ILogger<VaultController> _logger;

    public VaultController(
        IFileIngestService fileIngestService,
        IApplicationIngestionService applicationIngestionService,
        ILogger<VaultController> logger)
    {
        _fileIngestService = fileIngestService;
        _applicationIngestionService = applicationIngestionService;
        _logger = logger;
    }

    /// <summary>
    /// Async ingestion endpoint. Accepts a JSON payload from Claude, persists to MongoDB with
    /// status "Processing", publishes job.application.received, and returns 202 immediately.
    /// The Worker handles PDF conversion and GitHub commit.
    /// </summary>
    [HttpPost("ingest/applications")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IngestApplicationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestApplication(
        [FromBody] IngestApplicationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _applicationIngestionService.IngestAsync(request, cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Accepted(new IngestApplicationResponse { ApplicationId = result.ApplicationId! });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during application ingestion for {CompanyName}", request.CompanyName);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    // LEGACY: remove after async ingestion is confirmed stable.
    // This endpoint accepted multipart/form-data file uploads and committed them directly
    // to GitHub synchronously. Replaced by POST /api/ingest/applications + Worker pipeline.
    [HttpPost("ingest")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Ingest([FromForm] IngestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName))
                return BadRequest(new { error = "Company name is required" });

            if (request.Files == null || request.Files.Count == 0)
                return BadRequest(new { error = "At least one file must be uploaded" });

            var ingestedFiles = new List<IngestedFile>();
            foreach (var file in request.Files)
            {
                if (file.Length > 0)
                {
                    ingestedFiles.Add(new IngestedFile
                    {
                        FileName = file.FileName,
                        Content = file.OpenReadStream(),
                        Length = file.Length
                    });
                }
            }

            if (ingestedFiles.Count == 0)
                return BadRequest(new { error = "All uploaded files are empty" });

            var result = await _fileIngestService.IngestAsync(request.CompanyName, ingestedFiles, cancellationToken);

            if (!result.IsSuccess)
                return StatusCode(500, new { error = result.ErrorMessage });

            return Ok(new IngestResponse
            {
                CompanyName = result.CompanyName,
                CommitUrl = result.CommitUrl,
                FilesUploaded = result.FilesUploaded
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file ingestion for {CompanyName}", request.CompanyName);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}
