using JobVault.Contracts.Events;

namespace JobVault.Application.Interfaces;

/// <summary>
/// Service for sending notifications via Telegram.
/// </summary>
public interface ITelegramNotificationService
{
    /// <summary>
    /// Sends a notification about a job application event.
    /// </summary>
    /// <param name="jobEvent">The event to notify about.</param>
    Task SendNotificationAsync(JobApplicationEvent jobEvent);
}