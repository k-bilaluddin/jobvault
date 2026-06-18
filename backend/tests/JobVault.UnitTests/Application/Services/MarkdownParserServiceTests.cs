using JobVault.Application.Services;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Application.Services;

public class MarkdownParserServiceTests
{
    private readonly MarkdownParserService _sut;

    public MarkdownParserServiceTests()
    {
        var logger = Substitute.For<ILogger<MarkdownParserService>>();
        _sut = new MarkdownParserService(logger);
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_NullInput_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.ExtractJobApplicationAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_EmptyInput_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.ExtractJobApplicationAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_NoCodeBlock_ReturnsNull()
    {
        // Arrange
        var markdown = "# Some Heading\nJust regular markdown text with no code block.";

        // Act
        var result = await _sut.ExtractJobApplicationAsync(markdown);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_InvalidJson_ReturnsNull()
    {
        // Arrange
        var markdown = "```json\n{ invalid json }\n```";

        // Act
        var result = await _sut.ExtractJobApplicationAsync(markdown);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_EmptyCodeBlock_ReturnsNull()
    {
        // Arrange
        var markdown = "```json\n\n```";

        // Act
        var result = await _sut.ExtractJobApplicationAsync(markdown);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_ValidJsonBlock_ReturnsJobApplication()
    {
        // Arrange
        var markdown = """
            # Compatibility Report

            ```json
            { "companyName": "Acme Corp", "jobTitle": "Software Engineer", "matchScore": 85, "recommendation": "Strong Apply" }
            ```

            Some additional notes here.
            """;

        // Act
        var result = await _sut.ExtractJobApplicationAsync(markdown);

        // Assert
        result.Should().NotBeNull();
        result!.CompanyName.Should().Be("Acme Corp");
        result.JobTitle.Should().Be("Software Engineer");
        result.MatchScore.Should().Be(85);
        result.Recommendation.Should().Be("Strong Apply");
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_CaseInsensitiveProperties_ParsesCorrectly()
    {
        // Arrange
        var markdown = """
            ```json
            { "CompanyName": "TestCo", "JOBTITLE": "Dev", "matchscore": 50, "Recommendation": "Apply" }
            ```
            """;

        // Act
        var result = await _sut.ExtractJobApplicationAsync(markdown);

        // Assert
        result.Should().NotBeNull();
        result!.CompanyName.Should().Be("TestCo");
        result.JobTitle.Should().Be("Dev");
        result.MatchScore.Should().Be(50);
    }

    [Fact]
    public async Task ExtractJobApplicationAsync_MultipleCodeBlocks_OnlyParsesFirst()
    {
        // Arrange
        var markdown = """
            ```json
            { "companyName": "First Corp", "jobTitle": "Engineer" }
            ```

            ```json
            { "companyName": "Second Corp", "jobTitle": "Manager" }
            ```
            """;

        // Act
        var result = await _sut.ExtractJobApplicationAsync(markdown);

        // Assert
        result.Should().NotBeNull();
        result!.CompanyName.Should().Be("First Corp");
        result.JobTitle.Should().Be("Engineer");
    }
}
