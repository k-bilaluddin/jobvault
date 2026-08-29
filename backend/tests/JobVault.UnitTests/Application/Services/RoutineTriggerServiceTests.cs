using JobVault.Application.Interfaces;
using JobVault.Application.Services;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Application.Services;

public class RoutineTriggerServiceTests
{
    private readonly IRoutineTriggerClient _client;
    private readonly IRoutineTriggerStateStore _stateStore;
    private readonly IPendingJobService _pendingJobs;
    private readonly ILogger<RoutineTriggerService> _logger;
    private readonly RoutineTriggerService _sut;

    public RoutineTriggerServiceTests()
    {
        _client = Substitute.For<IRoutineTriggerClient>();
        _stateStore = Substitute.For<IRoutineTriggerStateStore>();
        _pendingJobs = Substitute.For<IPendingJobService>();
        _logger = Substitute.For<ILogger<RoutineTriggerService>>();

        _sut = new RoutineTriggerService(_client, _stateStore, _pendingJobs, _logger);
    }

    private static PendingJob AnyPendingJob() => new() { Id = "job1", Url = "https://example.com/job", Status = "pending" };

    // ─── TriggerAsync (manual) ──────────────────────────────────────

    [Fact]
    public async Task TriggerAsync_AlwaysFiresRegardlessOfGate_AndRecordsTimestamp()
    {
        // Arrange — pretend the routine just fired a minute ago; manual trigger should ignore that.
        _stateStore.GetLastTriggeredAtAsync(Arg.Any<CancellationToken>()).Returns(DateTime.UtcNow.AddMinutes(-1));

        // Act
        await _sut.TriggerAsync(CancellationToken.None);

        // Assert
        await _client.Received(1).RunAsync(Arg.Any<CancellationToken>());
        await _stateStore.Received(1).SetLastTriggeredAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // ─── TriggerIfDueAsync (scheduled) ──────────────────────────────

    [Fact]
    public async Task TriggerIfDueAsync_NeverTriggeredBefore_WithPendingJobs_Fires()
    {
        _stateStore.GetLastTriggeredAtAsync(Arg.Any<CancellationToken>()).Returns((DateTime?)null);
        _pendingJobs.GetPendingAsync(Arg.Any<CancellationToken>()).Returns(new List<PendingJob> { AnyPendingJob() });

        var fired = await _sut.TriggerIfDueAsync(CancellationToken.None);

        Assert.True(fired);
        await _client.Received(1).RunAsync(Arg.Any<CancellationToken>());
        await _stateStore.Received(1).SetLastTriggeredAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerIfDueAsync_LessThan24hSinceLastTrigger_DoesNotFire()
    {
        _stateStore.GetLastTriggeredAtAsync(Arg.Any<CancellationToken>()).Returns(DateTime.UtcNow.AddHours(-1));
        _pendingJobs.GetPendingAsync(Arg.Any<CancellationToken>()).Returns(new List<PendingJob> { AnyPendingJob() });

        var fired = await _sut.TriggerIfDueAsync(CancellationToken.None);

        Assert.False(fired);
        await _client.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().SetLastTriggeredAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerIfDueAsync_DueButNoPendingJobs_DoesNotFire()
    {
        _stateStore.GetLastTriggeredAtAsync(Arg.Any<CancellationToken>()).Returns(DateTime.UtcNow.AddDays(-2));
        _pendingJobs.GetPendingAsync(Arg.Any<CancellationToken>()).Returns(new List<PendingJob>());

        var fired = await _sut.TriggerIfDueAsync(CancellationToken.None);

        Assert.False(fired);
        await _client.DidNotReceive().RunAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerIfDueAsync_DueAndPendingJobsExist_Fires()
    {
        _stateStore.GetLastTriggeredAtAsync(Arg.Any<CancellationToken>()).Returns(DateTime.UtcNow.AddHours(-25));
        _pendingJobs.GetPendingAsync(Arg.Any<CancellationToken>()).Returns(new List<PendingJob> { AnyPendingJob() });

        var fired = await _sut.TriggerIfDueAsync(CancellationToken.None);

        Assert.True(fired);
        await _client.Received(1).RunAsync(Arg.Any<CancellationToken>());
    }
}
