using JobVault.Domain.Entities;
using JobVault.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Infrastructure.Notifications;

public class NotificationHubTests
{
    private readonly ILogger<NotificationHub> _logger = Substitute.For<ILogger<NotificationHub>>();
    private readonly NotificationHub _sut;

    public NotificationHubTests()
    {
        _sut = new NotificationHub(_logger);
    }

    private static AppNotification CreateNotification(string type = "new_application") => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Title = "Test notification",
        Body = "Test body",
        CompanyName = "TestCorp",
        CompanySlug = "testcorp",
        OccurredAt = DateTime.UtcNow,
        Read = false,
    };

    [Fact]
    public void Subscribe_ReturnsNonNullSubscriptionAndReader()
    {
        // Act
        var (subscription, reader) = _sut.Subscribe();

        // Assert
        subscription.Should().NotBeNull();
        reader.Should().NotBeNull();

        subscription.Dispose();
    }

    [Fact]
    public async Task BroadcastAsync_SingleSubscriber_ReceivesNotification()
    {
        // Arrange
        var (subscription, reader) = _sut.Subscribe();
        var notification = CreateNotification();

        // Act
        await _sut.BroadcastAsync(notification);

        // Assert
        var received = await reader.ReadAsync();
        received.Should().BeSameAs(notification);

        subscription.Dispose();
    }

    [Fact]
    public async Task BroadcastAsync_MultipleSubscribers_AllReceive()
    {
        // Arrange
        var (sub1, reader1) = _sut.Subscribe();
        var (sub2, reader2) = _sut.Subscribe();
        var notification = CreateNotification();

        // Act
        await _sut.BroadcastAsync(notification);

        // Assert
        var received1 = await reader1.ReadAsync();
        var received2 = await reader2.ReadAsync();
        received1.Should().BeSameAs(notification);
        received2.Should().BeSameAs(notification);

        sub1.Dispose();
        sub2.Dispose();
    }

    [Fact]
    public async Task Dispose_Subscription_CompletesChannel()
    {
        // Arrange
        var (subscription, reader) = _sut.Subscribe();

        // Act
        subscription.Dispose();

        // Assert
        await reader.Completion;
        reader.Completion.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_Subscription_RemovedFromBroadcast()
    {
        // Arrange
        var (sub1, reader1) = _sut.Subscribe();
        var (sub2, reader2) = _sut.Subscribe();
        var notification = CreateNotification();

        // Act — dispose first subscription, then broadcast
        sub1.Dispose();
        await _sut.BroadcastAsync(notification);

        // Assert — only second subscriber should receive the notification
        var received = await reader2.ReadAsync();
        received.Should().BeSameAs(notification);
        reader1.TryRead(out _).Should().BeFalse();

        sub2.Dispose();
    }

    [Fact]
    public async Task ConcurrentSubscribeAndBroadcast_NoExceptions()
    {
        // Arrange
        var exceptions = new List<Exception>();
        var notification = CreateNotification();

        // Act — run parallel subscribe/broadcast/dispose operations
        var act = async () =>
        {
            var tasks = new List<Task>();

            for (var i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var (sub, reader) = _sut.Subscribe();
                    await _sut.BroadcastAsync(notification);
                    sub.Dispose();
                }));
            }

            await Task.WhenAll(tasks);
        };

        // Assert
        await act.Should().NotThrowAsync();
    }
}
