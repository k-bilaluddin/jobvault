using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using JobVault.Contracts.Requests;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.Application.Services;

public class ApplicationIngestionService : IApplicationIngestionService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<ApplicationIngestionService> _logger;

    public ApplicationIngestionService(
        IJobApplicationRepository repository,
        IRabbitMqPublisher publisher,
        ILogger<ApplicationIngestionService> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ApplicationIngestionResult> IngestAsync(
        IngestApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationError = Validate(request);
            if (validationError != null)
                return ApplicationIngestionResult.Failure(validationError);

            var application = new JobApplication
            {
                CompanyName = request.CompanyName,
                JobTitle = request.JobTitle,
                Location = request.Location ?? string.Empty,
                JobUrl = request.JobUrl ?? string.Empty,
                WorkMode = request.WorkMode ?? string.Empty,
                EmploymentType = request.EmploymentType ?? string.Empty,
                SalaryMin = request.SalaryMin,
                SalaryMax = request.SalaryMax,
                Currency = request.Currency ?? "EUR",
                SalaryPeriod = request.SalaryPeriod ?? "Annual",
                MatchScore = request.MatchScore,
                Recommendation = request.Recommendation,
                Status = "Processing",
                JdSource = request.JdSource,
                Headline = request.Headline,
                Summary = request.Summary,
                Skills = request.Skills,
                Roles = request.Roles,
                Recipient = request.Recipient,
                CoverLetterParagraphs = request.CoverLetterParagraphs,
                Strengths = request.Strengths,
                Gaps = request.Gaps,
                TailoringNotes = request.TailoringNotes,
                CompatibilityReportMarkdown = request.CompatibilityReportMarkdown,
                TailoringNotesMarkdown = request.TailoringNotesMarkdown,
            };

            var upsertResult = await _repository.UpsertApplicationAsync(application);
            if (!upsertResult.IsSuccess || upsertResult.Id == null)
            {
                _logger.LogError("Failed to upsert application for {CompanyName}", request.CompanyName);
                return ApplicationIngestionResult.Failure("Failed to persist application");
            }

            try
            {
                var jobEvent = new JobApplicationEvent
                {
                    ApplicationId = upsertResult.Id,
                    CompanyName = application.CompanyName,
                    JobTitle = application.JobTitle,
                    MatchScore = application.MatchScore,
                    Recommendation = application.Recommendation,
                    Status = application.Status,
                    URL = application.JobUrl,
                    EventType = "received",
                    Timestamp = DateTime.UtcNow
                };

                await _publisher.PublishJobApplicationEventAsync(jobEvent);
            }
            catch (Exception ex)
            {
                // Application is already persisted — don't fail the request over RabbitMQ
                _logger.LogError(ex, "Failed to publish received event for {CompanyName}; returning 202 anyway", request.CompanyName);
            }

            _logger.LogInformation("Ingested application for {CompanyName}, id={Id}", request.CompanyName, upsertResult.Id);
            return ApplicationIngestionResult.Success(upsertResult.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error ingesting application for {CompanyName}", request.CompanyName);
            return ApplicationIngestionResult.Failure("An unexpected error occurred");
        }
    }

    private static string? Validate(IngestApplicationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return "companyName is required";
        if (string.IsNullOrWhiteSpace(request.JobTitle))
            return "jobTitle is required";
        if (string.IsNullOrWhiteSpace(request.Recommendation))
            return "recommendation is required";
        if (request.MatchScore < 0 || request.MatchScore > 100)
            return "matchScore must be between 0 and 100";
        if (string.IsNullOrWhiteSpace(request.Headline))
            return "headline is required";
        if (string.IsNullOrWhiteSpace(request.CompatibilityReportMarkdown))
            return "compatibilityReportMarkdown is required";
        if (string.IsNullOrWhiteSpace(request.TailoringNotesMarkdown))
            return "tailoringNotesMarkdown is required";
        return null;
    }
}
