using JobVault.Contracts.Responses;

namespace JobVault.Application.Interfaces;

public interface IGitSyncService
{
    GitSyncResponse Sync();
}
