using JobVault.API.Controllers;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.API.Controllers;

public class NotificationsControllerTests
{
    private readonly INotificationHub _notificationHub;
    private readonly INotificationQueryService _queryService;
    private readonly ILogger<NotificationsController> _logger;
    private readonly NotificationsController _sut;

    public NotificationsControllerTests()
    {
        _notificationHub = Substitute.For<INotificationHub>();
        _queryService = Substitute.For<INotificationQueryService>();
        _logger = Substitute.For<ILogger<NotificationsController>>();
        var tokenService = Substitute.For<ITokenService>();
        _sut = new NotificationsController(_notificationHub, _queryService, _logger, tokenService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task GetNotifications_ReturnsOkWithNotifications()
    {
        // Arrange
        var notifications = new List<NotificationResponse>
        {
            new() { Title = "Job processed", Body = "Acme application ready" },
            new() { Title = "New match", Body = "85% match found" }
        };
        _queryService.GetRecentAsync(50, Arg.Any<CancellationToken>()).Returns(notifications);

        // Act
        var result = await _sut.GetNotifications(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<IReadOnlyList<NotificationResponse>>().Subject;
        returned.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotifications_ServiceThrows_ExceptionBubbles()
    {
        // Arrange
        _queryService.GetRecentAsync(50, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("DB down"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _sut.GetNotifications(CancellationToken.None));
    }

    [Fact]
    public async Task MarkAllRead_ReturnsOk()
    {
        // Arrange
        _queryService.MarkAllReadAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(3L));

        // Act
        var result = await _sut.MarkAllRead(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _queryService.Received(1).MarkAllReadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkRead_ValidGuid_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _queryService.MarkReadAsync(id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.MarkRead(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
        await _queryService.Received(1).MarkReadAsync(id, Arg.Any<CancellationToken>());
    }
}
