using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using JobVault.Contracts.External.GitHub;
using Microsoft.Extensions.Logging;

namespace JobVault.Application.Services;

/// <summary>
/// Handles webhook payloads by orchestrating the processing of job applications.
/// </summary>
public class WebhookHandler : IWebhookHandler
{
    private readonly IGitHubFileService _gitHubFileService;
    private readonly IMarkdownParserService _markdownParserService;
    private readonly IJobApplicationRepository _repository;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<WebhookHandler> _logger;

    public WebhookHandler(
        IGitHubFileService gitHubFileService,
        IMarkdownParserService markdownParserService,
        IJobApplicationRepository repository,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<WebhookHandler> logger)
    {
        _gitHubFileService = gitHubFileService;
        _markdownParserService = markdownParserService;
        _repository = repository;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    public async Task<WebhookResult> HandleAsync(GitHubWebhookPayload payload)
    {
        try
        {
            _logger.LogInformation(
                "Processing webhook for repository {Repository}, ref {Ref}",
                payload.Repository.Name, payload.Ref);

            if (payload.Commits == null || payload.Commits.Count == 0)
            {
                _logger.LogInformation("No commits in webhook payload");
                return WebhookResult.Success("No commits to process");
            }

            var allFiles = new HashSet<string>();
            foreach (var commit in payload.Commits)
            {
                allFiles.UnionWith(commit.Added);
                allFiles.UnionWith(commit.Modified);
            }

            if (allFiles.Count == 0)
            {
                _logger.LogInformation("No files added or modified");
                return WebhookResult.Success("No files to process");
            }

            // Extract unique company names from file paths (first path segment)
            var companyNames = allFiles
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Split('/').FirstOrDefault())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            if (companyNames.Count == 0)
            {
                _logger.LogWarning("No valid company names extracted from file paths");
                return WebhookResult.Failure("No valid company names found in file paths");
            }

            _logger.LogInformation(
                "Processing {Count} company applications: {Companies}",
                companyNames.Count, string.Join(", ", companyNames));

            var processedCount = 0;
            var failedCount = 0;

            foreach (var companyName in companyNames)
            {
                var success = await ProcessCompanyApplicationAsync(companyName);
                if (success)
                {
                    processedCount++;
                }
                else
                {
                    failedCount++;
                }
            }

            var message = $"Processed {processedCount} application(s), {failedCount} failed";
            _logger.LogInformation(message);

            return processedCount > 0 
                ? WebhookResult.Success(message) 
                : WebhookResult.Failure(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook");
            return WebhookResult.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private async Task<bool> ProcessCompanyApplicationAsync(string companyName)
    {
        try
        {
            var filePath = $"{companyName}/compatibility-report.md";
            _logger.LogInformation("Fetching {FilePath}", filePath);

            var markdownContent = await _gitHubFileService.GetFileContentAsync(filePath);
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                _logger.LogWarning(
                    "Failed to fetch or empty content for {FilePath}",
                    filePath);
                return false;
            }

            var application = await _markdownParserService.ExtractJobApplicationAsync(markdownContent);
            if (application == null)
            {
                _logger.LogWarning(
                    "Failed to extract job application from {FilePath}",
                    filePath);
                return false;
            }

            var upsertResult = await _repository.UpsertApplicationAsync(application);
            if (!upsertResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to upsert job application for {CompanyName}",
                    companyName);
                return false;
            }

            _logger.LogInformation(
                "Successfully processed job application for {CompanyName}",
                companyName);

            // Publish event to RabbitMQ - don't fail webhook on RabbitMQ error
            try
            {
                var jobEvent = new JobApplicationEvent
                {
                    CompanyName = application.CompanyName,
                    JobTitle = application.JobTitle,
                    MatchScore = application.MatchScore,
                    Recommendation = application.Recommendation,
                    Status = application.Status,
                    URL = application.JobUrl,
                    EventType = upsertResult.IsNewDocument ? "created" : "updated",
                    Timestamp = DateTime.UtcNow
                };

                await _rabbitMqPublisher.PublishJobApplicationEventAsync(jobEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish RabbitMQ event for {CompanyName}, but continuing",
                    companyName);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing company application for {CompanyName}",
                companyName);
            return false;
        }
    }
}