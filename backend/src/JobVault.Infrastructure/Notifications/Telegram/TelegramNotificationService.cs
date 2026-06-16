using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JobVault.Infrastructure.Notifications.Telegram;

public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly TelegramBotClient? _botClient;
    private readonly long? _chatId;

    public TelegramNotificationService(IConfiguration configuration, ILogger<TelegramNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var botToken = _configuration["Telegram:BotToken"];
        var chatIdString = _configuration["Telegram:ChatId"];

        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogWarning("Telegram bot token not configured. Notifications will be disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(chatIdString) || !long.TryParse(chatIdString, out var parsedChatId))
        {
            _logger.LogWarning("Telegram chat ID not configured or invalid. Notifications will be disabled.");
            return;
        }

        try
        {
            _botClient = new TelegramBotClient(botToken);
            _chatId = parsedChatId;
            _logger.LogInformation("Telegram notification service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Telegram bot client");
        }
    }

    public async Task SendNotificationAsync(JobApplicationEvent jobEvent)
    {
        if (_botClient == null || _chatId == null)
        {
            _logger.LogWarning("Telegram notification service not initialized. Skipping notification.");
            return;
        }

        try
        {
            var message = FormatMessage(jobEvent);

            await _botClient.SendTextMessageAsync(
                chatId: _chatId.Value,
                text: message,
                parseMode: ParseMode.Html,
                disableNotification: false);

            _logger.LogInformation(
                "Sent Telegram notification for {CompanyName} with event type {EventType}",
                jobEvent.CompanyName, jobEvent.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send Telegram notification for {CompanyName} with event type {EventType}",
                jobEvent.CompanyName, jobEvent.EventType);
        }
    }

    private static string FormatMessage(JobApplicationEvent jobEvent)
    {
        return jobEvent.EventType.ToLower() switch
        {
            "created" => $"🆕 <b>Application Vaulted</b>\n\n" +
                         $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                         $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                         $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                         $"├ ✅ Status: <code>{jobEvent.Status}</code>\n" +
                         $"└ 🔗 <a href='{jobEvent.URL}'>View Job Posting</a>\n\n" +
                         $"<i>Processed at {DateTime.Now:HH:mm}</i>",

            "updated" => $"🔄 <b>Application Updated</b>\n\n" +
                         $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                         $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                         $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                         $"├ ✅ Status: <code>{jobEvent.Status}</code>\n" +
                         $"└ 🔗 <a href='{jobEvent.URL}'>Open Application</a>\n\n" +
                         $"<i>Updated at {DateTime.Now:HH:mm}</i>",

            "status_changed" => $"📌 <b>Status Changed</b>\n\n" +
                                $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                                $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                                $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                                $"├ 🔄 New Status: <code>{jobEvent.Status}</code>\n" +
                                $"└ 🔗 <a href='{jobEvent.URL}'>Open Application</a>\n\n" +
                                $"<i>Changed at {DateTime.Now:HH:mm}</i>",

            "interview_scheduled" => $"📅 <b>Interview Scheduled</b>\n\n" +
                                     $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                                     $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                                     $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                                     $"├ 🎉 Progress: <code>Interview Stage</code>\n" +
                                     $"└ 🔗 <a href='{jobEvent.URL}'>View Application</a>\n\n" +
                                     $"<i>Good luck!</i>",

            "offer_received" => $"🎉 <b>Offer Received</b>\n\n" +
                                 $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                                 $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                                 $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                                 $"├ 💼 Status: <code>Offer Received</code>\n" +
                                 $"└ 🔗 <a href='{jobEvent.URL}'>Review Application</a>\n\n" +
                                 $"<i>Congratulations!</i>",

            "rejected" => $"❌ <b>Application Rejected</b>\n\n" +
                          $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                          $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                          $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                          $"├ 🚫 Status: <code>Rejected</code>\n" +
                          $"└ 🔗 <a href='{jobEvent.URL}'>View Application</a>\n\n" +
                          $"<i>Keep applying. One rejection is not a trend.</i>",

            "deleted" => $"🗑️ <b>Application Removed</b>\n\n" +
                         $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                         $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                         $"└ 🧹 Removed from JobVault\n\n" +
                         $"<i>{DateTime.Now:HH:mm}</i>",

            _ => $"📋 <b>Application Event</b>\n\n" +
                 $"🏢 <b>{jobEvent.CompanyName}</b>\n" +
                 $"├ 📌 Event: <code>{jobEvent.EventType}</code>\n" +
                 $"├ 📊 Score: <code>{jobEvent.MatchScore}%</code>\n" +
                 $"├ 🎯 Priority: <code>{jobEvent.Recommendation}</code>\n" +
                 $"└ 🔗 <a href='{jobEvent.URL}'>Open Application</a>"
        };
    }
}