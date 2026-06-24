using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Vault;

public class GitSyncService : IGitSyncService
{
    private readonly IVaultFileService _vaultFileService;
    private readonly IJobApplicationRepository _repository;
    private readonly ILogger<GitSyncService> _logger;

    public GitSyncService(
        IVaultFileService vaultFileService,
        IJobApplicationRepository repository,
        ILogger<GitSyncService> logger)
    {
        _vaultFileService = vaultFileService;
        _repository = repository;
        _logger = logger;
    }

    public async Task<GitSyncResponse> SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

            foreach (var app in applications)
                _vaultFileService.EvictCache(app.CompanyName);

            _logger.LogInformation("Cache evicted for {Count} applications", applications.Count);

            return new GitSyncResponse { Ok = true, Message = $"Cache cleared for {applications.Count} applications. PDFs will be re-fetched from GitHub on next view." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync/cache eviction");
            return new GitSyncResponse { Ok = false, Message = ex.Message };
        }
    }
}
