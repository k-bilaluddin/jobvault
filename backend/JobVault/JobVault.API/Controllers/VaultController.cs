using JobVault.API.Models.Requests;
using JobVault.Application.Interfaces;
using JobVault.Application.Models;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api")]
public class VaultController : ControllerBase
{
    private readonly IFileIngestService _fileIngestService;
    private readonly ILogger<VaultController> _logger;

    public VaultController(
        IFileIngestService fileIngestService,
        ILogger<VaultController> logger)
    {
        _fileIngestService = fileIngestService;
        _logger = logger;
    }

    [HttpPost("ingest")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Ingest([FromForm] IngestRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate company name
            if (string.IsNullOrWhiteSpace(request.CompanyName))
            {
                return BadRequest(new { error = "Company name is required" });
            }

            // Validate files
            if (request.Files == null || request.Files.Count == 0)
            {
                return BadRequest(new { error = "At least one file must be uploaded" });
            }

            // Map IFormFile to IngestedFile
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
            {
                return BadRequest(new { error = "All uploaded files are empty" });
            }

            // Call the service
            var result = await _fileIngestService.IngestAsync(
                request.CompanyName, 
                ingestedFiles, 
                cancellationToken);

            // Return HTTP response based on result
            if (!result.IsSuccess)
            {
                return StatusCode(500, new { error = result.ErrorMessage });
            }

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