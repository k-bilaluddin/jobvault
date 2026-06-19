using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using JobVault.API.Logging;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JobVault.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationHub _notificationHub;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotificationsController(
        INotificationHub notificationHub,
        INotificationRepository notificationRepository,
        ILogger<NotificationsController> logger,
        IConfiguration configuration)
    {
        _notificationHub = notificationHub;
        _notificationRepository = notificationRepository;
        _logger = logger;
        _configuration = configuration;
    }

    // EventSource cannot send Authorization headers — accept token via query param for SSE only.
    [HttpGet("stream")]
    [AllowAnonymous]
    public async Task StreamNotifications([FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (!ValidateSseToken(token))
        {
            HttpContext.Response.StatusCode = 401;
            return;
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = HttpContext.TraceIdentifier
        });

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
            _logger.LogDebug(LogEvents.SseClientCancelled, "SSE stream cancelled for client {TraceId}", HttpContext.TraceIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.SseStreamError, ex, "Error in SSE stream for client {TraceId}", HttpContext.TraceIdentifier);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppNotification>>> GetNotifications()
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = HttpContext.TraceIdentifier
        });

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
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = HttpContext.TraceIdentifier
        });

        try
        {
            var count = await _notificationRepository.MarkAllReadAsync();
            _logger.LogInformation(LogEvents.NotificationsMarkedRead, "Marked {Count} notifications as read", count);
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
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = HttpContext.TraceIdentifier
        });

        try
        {
            await _notificationRepository.MarkReadAsync(id);
            _logger.LogInformation(LogEvents.NotificationMarkedRead, "Marked notification {Id} as read", id);
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

    private bool ValidateSseToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var secret = _configuration["Auth:JwtSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return false;

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            }, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
