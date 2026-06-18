using JobVault.Application.Common;

namespace JobVault.UnitTests.Application.Common;

public class FileIngestResultTests
{
    [Fact]
    public void Success_WithValidParameters_SetsIsSuccessTrueAndProperties()
    {
        // Arrange
        var filesUploaded = new List<string> { "resume.pdf", "cover-letter.md" };

        // Act
        var result = FileIngestResult.Success("Acme Corp", "https://github.com/commit/abc", filesUploaded);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CompanyName.Should().Be("Acme Corp");
        result.CommitUrl.Should().Be("https://github.com/commit/abc");
        result.FilesUploaded.Should().BeEquivalentTo(filesUploaded);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorMessage_SetsIsSuccessFalseAndErrorMessage()
    {
        // Arrange & Act
        var result = FileIngestResult.Failure("Upload failed");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Upload failed");
    }

    [Fact]
    public void Failure_WithErrorMessage_LeavesCompanyNameAsDefault()
    {
        // Arrange & Act
        var result = FileIngestResult.Failure("Upload failed");

        // Assert
        result.CompanyName.Should().BeEmpty();
        result.CommitUrl.Should().BeEmpty();
        result.FilesUploaded.Should().BeEmpty();
    }
}
