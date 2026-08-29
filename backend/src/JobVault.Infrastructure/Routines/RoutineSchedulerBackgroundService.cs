using JobVault.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Routines;

/// <summary>
/// Polls whether the job-queue routine is due to auto-fire: at most once every 24h, and only
/// when pending jobs exist. The gating logic itself lives in IRoutineTriggerService — this
/// class just owns the polling loop, checked every 30 minutes (so a 24h window doesn't drift
/// far past due, while staying cheap).
/// </summary>
public class RoutineSchedulerBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoutineSchedulerBackgroundService> _logger;

    public RoutineSchedulerBackgroundService(IServiceProvider serviceProvider, ILogger<RoutineSchedulerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        do
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var routineTriggerService = scope.ServiceProvider.GetRequiredService<IRoutineTriggerService>();
                await routineTriggerService.TriggerIfDueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job-queue routine auto-trigger check failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
