using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;

namespace JobVault.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private AppSettings? _cached;

    public SettingsService(ISettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<AppSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cached != null) return _cached;
        _cached = await _repository.GetAsync(cancellationToken) ?? new AppSettings();
        return _cached;
    }

    public async Task<AppSettings> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.SaveAsync(settings, cancellationToken);
        _cached = saved;
        return saved;
    }
}
