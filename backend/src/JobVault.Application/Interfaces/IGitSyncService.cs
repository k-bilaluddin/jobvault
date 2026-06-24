using JobVault.Contracts.Responses;

namespace JobVault.Application.Interfaces;

public interface IGitSyncService
{
    Task<GitSyncResponse> SyncAsync(CancellationToken cancellationToken = default);
}
