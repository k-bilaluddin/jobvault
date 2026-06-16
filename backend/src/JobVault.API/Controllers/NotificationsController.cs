using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JobVault.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationHub _notificationHub;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationsController> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotificationsController(
        INotificationHub notificationHub,
        INotificationRepository notificationRepository,
        ILogger<NotificationsController> logger)
    {
        _notificationHub = notificationHub;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    [HttpGet("stream")]
    public async Task StreamNotifications(CancellationToken cancellationToken)
    {
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
            // Client disconnected — normal shutdown path
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE stream");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppNotification>>> GetNotifications()
    {
        try
        {
            var notifications = await _notificationRepository.GetRecentAsync(50);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("read-all")]
    public async Task<ActionResult> MarkAllRead()
    {
        try
        {
            await _notificationRepository.MarkAllReadAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id)
    {
        try
        {
            await _notificationRepository.MarkReadAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task WriteRawAsync(string text, CancellationToken cancellationToken)
    {
        await Response.WriteAsync(text, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
