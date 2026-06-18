using JobVault.API.Controllers;
using JobVault.Application.Interfaces;
using JobVault.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.API.Controllers;

public class NotificationsControllerTests
{
    private readonly INotificationHub _notificationHub;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationsController> _logger;
    private readonly NotificationsController _sut;

    public NotificationsControllerTests()
    {
        _notificationHub = Substitute.For<INotificationHub>();
        _notificationRepository = Substitute.For<INotificationRepository>();
        _logger = Substitute.For<ILogger<NotificationsController>>();
        _sut = new NotificationsController(_notificationHub, _notificationRepository, _logger);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOkWithNotifications()
    {
        // Arrange
        var notifications = new List<AppNotification>
        {
            new() { Title = "Job processed", Body = "Acme application ready" },
            new() { Title = "New match", Body = "85% match found" }
        };
        _notificationRepository.GetRecentAsync(50).Returns(notifications);

        // Act
        var result = await _sut.GetNotifications();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<IEnumerable<AppNotification>>().Subject;
        returned.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotifications_RepositoryThrows_Returns500()
    {
        // Arrange
        _notificationRepository.GetRecentAsync(50).ThrowsAsync(new Exception("DB down"));

        // Act
        var result = await _sut.GetNotifications();

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task MarkAllRead_ReturnsOk()
    {
        // Arrange
        _notificationRepository.MarkAllReadAsync().Returns(Task.CompletedTask);

        // Act
        var result = await _sut.MarkAllRead();

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationRepository.Received(1).MarkAllReadAsync();
    }

    [Fact]
    public async Task MarkRead_ValidGuid_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _notificationRepository.MarkReadAsync(id).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.MarkRead(id);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _notificationRepository.Received(1).MarkReadAsync(id);
    }
}
