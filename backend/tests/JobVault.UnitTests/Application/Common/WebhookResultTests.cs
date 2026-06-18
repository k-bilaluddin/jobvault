using JobVault.Application.Common;

namespace JobVault.UnitTests.Application.Common;

public class WebhookResultTests
{
    [Fact]
    public void Success_WithMessage_SetsIsSuccessTrueAndMessage()
    {
        // Arrange & Act
        var result = WebhookResult.Success("Processed 3 applications");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Processed 3 applications");
    }

    [Fact]
    public void Failure_WithMessage_SetsIsSuccessFalseAndMessage()
    {
        // Arrange & Act
        var result = WebhookResult.Failure("No valid company names found");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("No valid company names found");
    }

    [Fact]
    public void Success_AndFailure_HaveDifferentIsSuccessValues()
    {
        // Arrange & Act
        var success = WebhookResult.Success("ok");
        var failure = WebhookResult.Failure("error");

        // Assert
        success.IsSuccess.Should().NotBe(failure.IsSuccess);
    }
}
