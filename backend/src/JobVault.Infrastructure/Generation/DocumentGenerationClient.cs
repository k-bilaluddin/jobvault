using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Generation;

public sealed class DocumentGenerationClient : IDocumentGenerationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<DocumentGenerationClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public DocumentGenerationClient(HttpClient http, ILogger<DocumentGenerationClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<byte[]> GenerateCvAsync(JobApplication application, CancellationToken cancellationToken = default) =>
        PostAsync("/api/generate-cv", BuildPayload(application), cancellationToken);

    public Task<byte[]> GenerateCoverLetterAsync(JobApplication application, CancellationToken cancellationToken = default) =>
        PostAsync("/api/generate-cover-letter", BuildPayload(application), cancellationToken);

    private async Task<byte[]> PostAsync(string path, GenerationPayload payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calling generation service: POST {Path} for {Company}", path, payload.Company);

        using var response = await _http.PostAsJsonAsync(path, payload, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var message = $"Generation service returned {(int)response.StatusCode} for {path}: {body}";

            // 4xx = bad payload — retrying won't help; throw InvalidOperationException so the
            // consumer skips the retry loop and dead-letters immediately.
            // 5xx / network errors = transient; throw HttpRequestException so retries fire.
            if ((int)response.StatusCode is >= 400 and < 500)
                throw new InvalidOperationException(message);

            throw new HttpRequestException(message);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        _logger.LogInformation("Generation service returned {Bytes} bytes for {Path}", bytes.Length, path);
        return bytes;
    }

    private static GenerationPayload BuildPayload(JobApplication app) => new()
    {
        Company = app.CompanyName,
        Role = app.JobTitle,
        JdSource = app.JdSource,
        Headline = app.Headline ?? string.Empty,
        Summary = app.Summary ?? string.Empty,
        Skills = app.Skills,
        Roles = app.Roles,
        Recipient = app.Recipient,
        CoverLetterParagraphs = app.CoverLetterParagraphs,
        CompatibilityScore = app.MatchScore,
        Strengths = app.Strengths,
        Gaps = app.Gaps,
        TailoringNotes = app.TailoringNotes,
    };
}

