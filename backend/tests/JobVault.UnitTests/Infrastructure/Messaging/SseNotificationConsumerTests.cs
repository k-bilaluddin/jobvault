using JobVault.Contracts.Events;
using JobVault.Infrastructure.Messaging.RabbitMQ;

namespace JobVault.UnitTests.Infrastructure.Messaging;

public class SseNotificationConsumerTests
{
    private static JobApplicationEvent CreateEvent(
        string eventType = "created",
        string companyName = "Acme Corp",
        int matchScore = 85,
        string recommendation = "Strong Apply",
        string status = "Ready to Apply") => new()
    {
        ApplicationId = "test-id",
        CompanyName = companyName,
        JobTitle = "Engineer",
        MatchScore = matchScore,
        Recommendation = recommendation,
        Status = status,
        URL = "https://example.com/job",
        EventType = eventType,
        Timestamp = DateTime.UtcNow,
    };

    [Fact]
    public void BuildNotification_CreatedEvent_SetsTypeNewApplication()
    {
        // Arrange
        var jobEvent = CreateEvent(eventType: "created");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.Type.Should().Be("new_application");
    }

    [Fact]
    public void BuildNotification_UpdatedEvent_SetsTypeStageChanged()
    {
        // Arrange
        var jobEvent = CreateEvent(eventType: "updated");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.Type.Should().Be("stage_changed");
    }

    [Fact]
    public void BuildNotification_UnknownEvent_SetsTypeSyncCompleted()
    {
        // Arrange
        var jobEvent = CreateEvent(eventType: "foo");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.Type.Should().Be("sync_completed");
    }

    [Fact]
    public void BuildNotification_IncludesCompanyInTitle()
    {
        // Arrange
        var jobEvent = CreateEvent(companyName: "MegaCorp");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.Title.Should().Contain("MegaCorp");
    }

    [Fact]
    public void BuildNotification_CreatedEvent_BodyContainsScoreAndRecommendation()
    {
        // Arrange
        var jobEvent = CreateEvent(
            eventType: "created",
            matchScore: 92,
            recommendation: "Strong Apply");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.Body.Should().Contain("92");
        notification.Body.Should().Contain("Strong Apply");
    }

    [Fact]
    public void BuildNotification_CompanySlug_SpacesToHyphens()
    {
        // Arrange
        var jobEvent = CreateEvent(companyName: "Acme Corp");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.CompanySlug.Should().Be("acme-corp");
    }

    [Fact]
    public void BuildNotification_CompanySlug_RemovesSpecialChars()
    {
        // Arrange
        var jobEvent = CreateEvent(companyName: "O'Reilly, Inc.");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.CompanySlug.Should().Be("oreilly-inc");
    }

    [Fact]
    public void BuildNotification_CompanySlug_TrimsTrailingHyphen()
    {
        // Arrange
        var jobEvent = CreateEvent(companyName: "Test Corp.");

        // Act
        var notification = SseNotificationConsumer.BuildNotification(jobEvent);

        // Assert
        notification.CompanySlug.Should().Be("test-corp");
    }
}
