using JobVault.Application.Common;

namespace JobVault.UnitTests.Application.Common;

public class ApplicationIngestionResultTests
{
    [Fact]
    public void Success_WithApplicationId_SetsIsSuccessTrue()
    {
        // Arrange & Act
        var result = ApplicationIngestionResult.Success("abc123");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_WithApplicationId_SetsApplicationIdToValue()
    {
        // Arrange & Act
        var result = ApplicationIngestionResult.Success("abc123");

        // Assert
        result.ApplicationId.Should().Be("abc123");
    }

    [Fact]
    public void Success_WithApplicationId_SetsErrorMessageToNull()
    {
        // Arrange & Act
        var result = ApplicationIngestionResult.Success("abc123");

        // Assert
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorMessage_SetsIsSuccessFalseAndErrorMessage()
    {
        // Arrange & Act
        var result = ApplicationIngestionResult.Failure("Something went wrong");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Something went wrong");
        result.ApplicationId.Should().BeNull();
    }
}
