using JobVault.Domain.Entities;

namespace JobVault.Application.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
