using System.Text.Json;
using JobVault.API.Logging;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[Authorize]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationHub _notificationHub;
    private readonly INotificationQueryService _queryService;
    private readonly ILogger<NotificationsController> _logger;
    private readonly ITokenService _tokenService;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotificationsController(
        INotificationHub notificationHub,
        INotificationQueryService queryService,
        ILogger<NotificationsController> logger,
        ITokenService tokenService)
    {
        _notificationHub = notificationHub;
        _queryService = queryService;
        _logger = logger;
        _tokenService = tokenService;
    }

    [HttpGet("stream")]
    [AllowAnonymous]
    public async Task StreamNotifications([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (!_tokenService.ValidateToken(token))
        {
            HttpContext.Response.StatusCode = 401;
            return;
        }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");
        Response.Headers.Append("Connection", "keep-alive");

        var (subscription, reader) = _notificationHub.Subscribe();
        using var _ = subscription;

        await WriteRawAsync(": connected\n\n", cancellationToken);

        using var pingTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        var pingTask = Task.Run(async () =>
        {
            try
            {
                while (await pingTimer.WaitForNextTickAsync(cancellationToken))
                {
                    await WriteRawAsync(": ping\n\n", cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }, cancellationToken);

        try
        {
            await foreach (var notification in reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(notification, _jsonOptions);
                await WriteRawAsync($"data: {json}\n\n", cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(LogEvents.SseClientCancelled, "SSE stream cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.SseStreamError, ex, "Error in SSE stream");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotifications(CancellationToken cancellationToken)
    {
        var result = await _queryService.GetRecentAsync(50, cancellationToken);
        return Ok(result);
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _queryService.MarkAllReadAsync(cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _queryService.MarkReadAsync(id, cancellationToken);
        return Ok();
    }

    private async Task WriteRawAsync(string text, CancellationToken cancellationToken)
    {
        await Response.WriteAsync(text, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
