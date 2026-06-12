using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

/// <summary>
/// Service for parsing markdown content to extract structured data.
/// </summary>
public interface IMarkdownParserService
{
    /// <summary>
    /// Extracts a job application from markdown content containing JSON.
    /// </summary>
    /// <param name="markdownContent">The markdown content to parse.</param>
    /// <returns>The extracted job application or null if parsing fails.</returns>
    Task<JobApplication?> ExtractJobApplicationAsync(string markdownContent);
}