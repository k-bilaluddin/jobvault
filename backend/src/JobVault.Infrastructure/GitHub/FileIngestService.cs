using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.GitHub;

public class FileIngestService : IFileIngestService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileIngestService> _logger;

    public FileIngestService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<FileIngestService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FileIngestResult> IngestAsync(
        string companyName,
        IReadOnlyCollection<IngestedFile> files,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = _configuration["GitHub:Token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("GitHub token not configured");
                return FileIngestResult.Failure("GitHub token not configured");
            }

            var owner = _configuration["GitHub:Owner"] ?? "k-bilaluddin";
            var repo = _configuration["GitHub:Repository"] ?? "job-applications-vault";
            var branch = _configuration["GitHub:Branch"] ?? "master";

            var uploadedFiles = new List<string>();
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobVault.API/1.0");

            // Get the latest commit SHA for the branch
            var latestSha = await GetLatestCommitShaAsync(client, owner, repo, branch, cancellationToken);
            if (latestSha == null)
            {
                return FileIngestResult.Failure("Failed to get latest commit SHA");
            }

            // Create tree entries for all files
            var treeEntries = new List<object>();
            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                var fileName = Path.GetFileName(file.FileName);
                var filePath = $"{companyName}/{fileName}";

                // Read file content and convert to base64
                using var memoryStream = new MemoryStream();
                await file.Content.CopyToAsync(memoryStream, cancellationToken);
                var content = Convert.ToBase64String(memoryStream.ToArray());

                // Create blob
                var blobSha = await CreateBlobAsync(client, owner, repo, content, cancellationToken);
                if (blobSha == null)
                {
                    return FileIngestResult.Failure($"Failed to create blob for {fileName}");
                }

                treeEntries.Add(new
                {
                    path = filePath,
                    mode = "100644",
                    type = "blob",
                    sha = blobSha
                });

                uploadedFiles.Add(filePath);
            }

            // Create a tree
            var treeSha = await CreateTreeAsync(client, owner, repo, latestSha, treeEntries, cancellationToken);
            if (treeSha == null)
            {
                return FileIngestResult.Failure("Failed to create tree");
            }

            // Create a commit
            var commitMessage = $"Added {companyName} application files";
            var commitSha = await CreateCommitAsync(
                client, owner, repo, commitMessage, treeSha, latestSha, cancellationToken);
            if (commitSha == null)
            {
                return FileIngestResult.Failure("Failed to create commit");
            }

            // Update the reference
            var updated = await UpdateReferenceAsync(client, owner, repo, branch, commitSha, cancellationToken);
            if (!updated)
            {
                return FileIngestResult.Failure("Failed to update reference");
            }

            var commitUrl = $"https://github.com/{owner}/{repo}/commit/{commitSha}";

            _logger.LogInformation(
                "Successfully ingested {FileCount} files for {CompanyName}. Commit: {CommitUrl}",
                uploadedFiles.Count, companyName, commitUrl);

            return FileIngestResult.Success(companyName, commitUrl, uploadedFiles);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GitHub API request failed");
            return FileIngestResult.Failure("Failed to communicate with GitHub API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file ingestion");
            return FileIngestResult.Failure("An unexpected error occurred");
        }
    }

    private async Task<string?> GetLatestCommitShaAsync(
        HttpClient client, string owner, string repo, string branch, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/git/refs/heads/{branch}";
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get reference: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("object").GetProperty("sha").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest commit SHA");
            return null;
        }
    }

    private async Task<string?> CreateBlobAsync(
        HttpClient client, string owner, string repo, string content, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/git/blobs";
            var payload = new
            {
                content,
                encoding = "base64"
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, httpContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create blob: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("sha").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blob");
            return null;
        }
    }

    private async Task<string?> CreateTreeAsync(
        HttpClient client, string owner, string repo, string baseSha, List<object> tree, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/git/trees";
            var payload = new
            {
                base_tree = baseSha,
                tree
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, httpContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create tree: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("sha").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tree");
            return null;
        }
    }

    private async Task<string?> CreateCommitAsync(
        HttpClient client, string owner, string repo, string message,
        string treeSha, string parentSha, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/git/commits";
            var payload = new
            {
                message,
                tree = treeSha,
                parents = new[] { parentSha }
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, httpContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create commit: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("sha").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating commit");
            return null;
        }
    }

    private async Task<bool> UpdateReferenceAsync(
        HttpClient client, string owner, string repo, string branch, string sha, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/git/refs/heads/{branch}";
            var payload = new
            {
                sha,
                force = false
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PatchAsync(url, httpContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to update reference: {StatusCode}", response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reference");
            return false;
        }
    }
}