using System.Collections.Concurrent;
using System.Net.Http.Headers;
using JobVault.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Vault;

public class VaultFileService : IVaultFileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<VaultFileService> _logger;
    private readonly ConcurrentDictionary<string, byte[]> _pdfCache = new();

    public VaultFileService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ISettingsService settingsService,
        ILogger<VaultFileService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _settingsService = settingsService;
        _logger = logger;
    }

    public (bool HasCvPdf, bool HasLetterPdf, bool HasReport, bool HasNotes) CheckFiles(string companyName)
    {
        return (false, false, false, false);
    }

    public string? ReadMarkdown(string companyName, string[] fileNames)
    {
        return null;
    }

    public async Task<byte[]?> GetPdfBytesAsync(string companyName, string type, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);

        var fileName = type == "cv" ? settings.GitHubCvFileName : settings.GitHubCoverLetterFileName;
        var cacheKey = $"{companyName}/{fileName}.pdf";

        if (_pdfCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var token = _configuration["GitHub:Token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError("GitHub token not configured");
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobVault.API/1.0");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));

            var path = $"{companyName}/{fileName}.pdf";
            var url = $"https://api.github.com/repos/{settings.GitHubOwner}/{settings.GitHubRepository}/contents/{Uri.EscapeDataString(path)}?ref={settings.GitHubBranch}";

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned {StatusCode} for {Path}", response.StatusCode, path);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            _pdfCache[cacheKey] = bytes;

            _logger.LogInformation("Fetched and cached PDF from GitHub: {Path} ({Bytes} bytes)", path, bytes.Length);
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PDF from GitHub for {CompanyName}/{Type}", companyName, type);
            return null;
        }
    }

    public void EvictCache(string companyName)
    {
        var keysToRemove = _pdfCache.Keys.Where(k => k.StartsWith($"{companyName}/")).ToList();
        foreach (var key in keysToRemove)
            _pdfCache.TryRemove(key, out _);

        if (keysToRemove.Count > 0)
            _logger.LogInformation("Evicted {Count} cached PDF(s) for {CompanyName}", keysToRemove.Count, companyName);
    }
}
