using JobVault.Application.Common;

namespace JobVault.UnitTests.Application.Common;

public class UpsertResultTests
{
    [Fact]
    public void Success_WithNewDocument_SetsIsSuccessTrueAndIsNewDocumentTrue()
    {
        // Arrange & Act
        var result = UpsertResult.Success(isNewDocument: true, id: "doc123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsNewDocument.Should().BeTrue();
        result.Id.Should().Be("doc123");
    }

    [Fact]
    public void Success_WithExistingDocument_SetsIsNewDocumentFalse()
    {
        // Arrange & Act
        var result = UpsertResult.Success(isNewDocument: false, id: "doc456");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsNewDocument.Should().BeFalse();
        result.Id.Should().Be("doc456");
    }

    [Fact]
    public void Success_WithoutId_SetsIdToNull()
    {
        // Arrange & Act
        var result = UpsertResult.Success(isNewDocument: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Id.Should().BeNull();
    }

    [Fact]
    public void Failure_SetsIsSuccessFalseAndIsNewDocumentFalse()
    {
        // Arrange & Act
        var result = UpsertResult.Failure();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsNewDocument.Should().BeFalse();
    }
}
