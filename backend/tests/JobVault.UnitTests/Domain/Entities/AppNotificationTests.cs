using JobVault.Domain.Entities;

namespace JobVault.UnitTests.Domain.Entities;

public class AppNotificationTests
{
    [Fact]
    public void NewNotification_ReadDefaultsToFalse()
    {
        // Arrange & Act
        var notification = new AppNotification();

        // Assert
        notification.Read.Should().BeFalse();
    }

    [Fact]
    public void NewNotification_IdIsValidGuid()
    {
        // Arrange & Act
        var notification = new AppNotification();

        // Assert
        notification.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewNotification_OccurredAtIsRecentUtc()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var notification = new AppNotification();
        var after = DateTime.UtcNow;

        // Assert
        notification.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after.AddSeconds(5));
    }
}
