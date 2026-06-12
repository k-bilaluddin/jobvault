using System.Net.Http.Headers;
using System.Text.Json;
using JobVault.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.GitHub;

public class GitHubFileService : IGitHubFileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubFileService> _logger;

    public GitHubFileService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GitHubFileService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetFileContentAsync(string path)
    {
        try
        {
            var token = _configuration["GitHub:Token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("GitHub token not configured");
                return null;
            }

            var owner = _configuration["GitHub:Owner"] ?? "k-bilaluddin";
            var repo = _configuration["GitHub:Repository"] ?? "job-applications-vault";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobVault.API/1.0");

            var url = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch file {Path} from GitHub. Status: {StatusCode}",
                    path, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            
            if (!doc.RootElement.TryGetProperty("content", out var contentElement))
            {
                _logger.LogWarning("No content property found in GitHub API response for {Path}", path);
                return null;
            }

            var base64Content = contentElement.GetString();
            if (string.IsNullOrWhiteSpace(base64Content))
            {
                _logger.LogWarning("Empty content returned for {Path}", path);
                return null;
            }

            // GitHub returns base64 with newlines, remove them before decoding
            base64Content = base64Content.Replace("\n", "").Replace("\r", "");
            var bytes = Convert.FromBase64String(base64Content);
            var decodedContent = System.Text.Encoding.UTF8.GetString(bytes);

            _logger.LogInformation("Successfully fetched file content for {Path}", path);
            return decodedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file content for {Path}", path);
            return null;
        }
    }
}