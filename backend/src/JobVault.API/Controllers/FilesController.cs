using JobVault.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IGitHubFileService _gitHubFileService;
    private readonly IJobApplicationRepository _repository;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IGitHubFileService gitHubFileService,
        IJobApplicationRepository repository,
        ILogger<FilesController> logger)
    {
        _gitHubFileService = gitHubFileService;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Proxies files from GitHub with proper content type and caching headers.
    /// </summary>
    /// <param name="id">The application ID.</param>
    /// <param name="filename">The filename to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content with appropriate headers.</returns>
    [HttpGet("{id}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFile(
        string id,
        string filename,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate application exists
            var application = await _repository.GetApplicationByIdAsync(id);
            if (application == null)
            {
                return NotFound(new { error = $"Application with ID '{id}' not found" });
            }

            // Construct GitHub path
            var path = $"{application.CompanyName}/{filename}";
            var content = await _gitHubFileService.GetFileContentAsync(path);

            if (content == null)
            {
                return NotFound(new { error = $"File '{filename}' not found" });
            }

            // Determine content type
            var contentType = filename.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                ? "text/markdown"
                : filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? "application/pdf"
                    : "application/octet-stream";

            // Add caching headers (cache for 1 hour)
            Response.Headers.CacheControl = "public, max-age=3600";
            Response.Headers.ETag = $"\"{ComputeETag(content)}\"";

            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return File(bytes, contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {Filename} for application {Id}", filename, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static string ComputeETag(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }
}