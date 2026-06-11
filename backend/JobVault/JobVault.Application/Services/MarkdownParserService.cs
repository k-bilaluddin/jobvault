using System.Text.Json;
using System.Text.RegularExpressions;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.Application.Services;

/// <summary>
/// Parses markdown content to extract job application data.
/// </summary>
public class MarkdownParserService : IMarkdownParserService
{
    private readonly ILogger<MarkdownParserService> _logger;

    public MarkdownParserService(ILogger<MarkdownParserService> logger)
    {
        _logger = logger;
    }

    public Task<JobApplication?> ExtractJobApplicationAsync(string markdownContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                _logger.LogWarning("Empty markdown content provided");
                return Task.FromResult<JobApplication?>(null);
            }

            // Match ```json ... ``` code block
            var pattern = @"```json\s*([\s\S]*?)\s*```";
            var match = Regex.Match(markdownContent, pattern, RegexOptions.Multiline);

            if (!match.Success)
            {
                _logger.LogWarning("No JSON code block found in markdown content");
                return Task.FromResult<JobApplication?>(null);
            }

            var jsonContent = match.Groups[1].Value.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var application = JsonSerializer.Deserialize<JobApplication>(jsonContent, options);

            if (application == null)
            {
                _logger.LogWarning("Failed to deserialize JSON content into JobApplication");
                return Task.FromResult<JobApplication?>(null);
            }

            _logger.LogInformation(
                "Successfully extracted job application for {CompanyName}",
                application.CompanyName);

            return Task.FromResult<JobApplication?>(application);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while parsing markdown");
            return Task.FromResult<JobApplication?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting job application from markdown");
            return Task.FromResult<JobApplication?>(null);
        }
    }
}