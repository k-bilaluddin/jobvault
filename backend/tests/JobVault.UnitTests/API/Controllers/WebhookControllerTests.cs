using JobVault.API.Controllers;
using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Contracts.External.GitHub;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.API.Controllers;

public class WebhookControllerTests
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookController> _logger;
    private readonly WebhookController _sut;

    public WebhookControllerTests()
    {
        _webhookHandler = Substitute.For<IWebhookHandler>();
        _logger = Substitute.For<ILogger<WebhookController>>();
        _sut = new WebhookController(_webhookHandler, _logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task HandleGitHubWebhook_Success_Returns200WithResponse()
    {
        // Arrange
        var payload = new GitHubWebhookPayload();
        _webhookHandler.HandleAsync(payload).Returns(WebhookResult.Success("Processed 1"));

        // Act
        var result = await _sut.HandleGitHubWebhook(payload);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<WebhookResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Processed 1");
    }

    [Fact]
    public async Task HandleGitHubWebhook_Failure_Returns400()
    {
        // Arrange
        var payload = new GitHubWebhookPayload();
        _webhookHandler.HandleAsync(payload).Returns(WebhookResult.Failure("No valid company names"));

        // Act
        var result = await _sut.HandleGitHubWebhook(payload);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<WebhookResponse>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("No valid company names");
    }

    [Fact]
    public async Task HandleGitHubWebhook_HandlerThrows_ExceptionBubbles()
    {
        // Arrange
        var payload = new GitHubWebhookPayload();
        _webhookHandler.HandleAsync(payload).ThrowsAsync(new Exception("Boom"));

        // Act & Assert — exception bubbles to GlobalExceptionHandler middleware
        await Assert.ThrowsAsync<Exception>(() =>
            _sut.HandleGitHubWebhook(payload));
    }

    [Fact]
    public async Task HandleGitHubWebhook_PassesPayloadToHandler()
    {
        // Arrange
        var payload = new GitHubWebhookPayload
        {
            Ref = "refs/heads/main",
            Repository = new RepositoryInfo { Name = "test-repo" }
        };
        _webhookHandler.HandleAsync(payload).Returns(WebhookResult.Success("OK"));

        // Act
        await _sut.HandleGitHubWebhook(payload);

        // Assert
        await _webhookHandler.Received(1).HandleAsync(payload);
    }
}
