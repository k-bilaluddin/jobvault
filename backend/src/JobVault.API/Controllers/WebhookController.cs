using JobVault.Application.Interfaces;
using JobVault.Contracts.External.GitHub;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

// LEGACY: remove after async ingestion is confirmed stable in production.
// Superseded by POST /api/ingest/applications + Worker pipeline.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookHandler webhookHandler,
        ILogger<WebhookController> logger)
    {
        _webhookHandler = webhookHandler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<WebhookResponse>> HandleGitHubWebhook(
        [FromBody] GitHubWebhookPayload payload)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = HttpContext.TraceIdentifier
        });

        try
        {
            _logger.LogInformation("Received GitHub webhook");

            var result = await _webhookHandler.HandleAsync(payload);

            var response = new WebhookResponse
            {
                Success = result.IsSuccess,
                Message = result.Message
            };

            return result.IsSuccess ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in webhook controller");
            return StatusCode(500, new WebhookResponse
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }
}