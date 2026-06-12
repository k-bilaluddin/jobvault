namespace JobVault.Application.Interfaces;

/// <summary>
/// Service for fetching files from GitHub repositories.
/// </summary>
public interface IGitHubFileService
{
    /// <summary>
    /// Gets the content of a file from a GitHub repository.
    /// </summary>
    /// <param name="path">The path to the file in the repository.</param>
    /// <returns>The file content as a string, or null if not found.</returns>
    Task<string?> GetFileContentAsync(string path);
}