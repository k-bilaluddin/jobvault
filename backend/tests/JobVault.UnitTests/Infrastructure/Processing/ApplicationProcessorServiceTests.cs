using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;
using JobVault.Infrastructure.Processing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Infrastructure.Processing;

public class ApplicationProcessorServiceTests
{
    private readonly IJobApplicationRepository _repository = Substitute.For<IJobApplicationRepository>();
    private readonly IDocumentGenerationClient _generationClient = Substitute.For<IDocumentGenerationClient>();
    private readonly IFileIngestService _fileIngestService = Substitute.For<IFileIngestService>();
    private readonly IRabbitMqPublisher _publisher = Substitute.For<IRabbitMqPublisher>();
    private readonly ILogger<ApplicationProcessorService> _logger = Substitute.For<ILogger<ApplicationProcessorService>>();
    private readonly ApplicationProcessorService _sut;

    public ApplicationProcessorServiceTests()
    {
        var config = CreateConfig();

        _generationClient.GenerateCvAsync(Arg.Any<JobApplication>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });
        _generationClient.GenerateCoverLetterAsync(Arg.Any<JobApplication>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });

        _sut = new ApplicationProcessorService(
            _repository,
            _generationClient,
            _fileIngestService,
            _publisher,
            config,
            _logger);
    }

    private static IConfiguration CreateConfig(Dictionary<string, string?>? overrides = null)
    {
        var defaults = new Dictionary<string, string?>
        {
            ["GitHub:CvFileName"] = "TestCV",
            ["GitHub:CoverLetterFileName"] = "TestCoverLetter",
            ["LibreOffice:ExecutablePath"] = "libreoffice",
        };
        if (overrides != null)
            foreach (var kv in overrides)
                defaults[kv.Key] = kv.Value;
        return new ConfigurationBuilder().AddInMemoryCollection(defaults).Build();
    }

    private static JobApplication CreateValidApplication() => new()
    {
        Id = "abc123",
        CompanyName = "Acme Corp",
        JobTitle = "Engineer",
        MatchScore = 80,
        Recommendation = "Strong Apply",
        JobUrl = "https://example.com/job",
        Headline = "Test headline",
        CompatibilityReportMarkdown = "# Compatibility Report",
        TailoringNotesMarkdown = "# Tailoring Notes",
        Skills = [new() { Label = "C#", Value = "Expert" }],
        Roles = [new() { Id = "calvergy", Bullets = ["Led team"] }],
    };

    [Fact]
    public async Task ProcessAsync_ApplicationNotFound_UpdatesStatusToFailed()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((JobApplication?)null);

        // Act
        await _sut.ProcessAsync("missing-id");

        // Assert
        await _repository.Received(1).UpdateStatusAsync(
            "missing-id",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MissingHeadline_CallsFailAsync()
    {
        // Arrange
        var app = CreateValidApplication();
        app.Headline = null;
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act
        await _sut.ProcessAsync("abc123");

        // Assert
        await _repository.Received(1).UpdateStatusAsync(
            "abc123",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Is<string?>(s => s != null && s.Contains("required fields")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MissingCompatibilityReport_CallsFailAsync()
    {
        // Arrange
        var app = CreateValidApplication();
        app.CompatibilityReportMarkdown = null;
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act
        await _sut.ProcessAsync("abc123");

        // Assert
        await _repository.Received(1).UpdateStatusAsync(
            "abc123",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Is<string?>(s => s != null && s.Contains("required fields")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MissingTailoringNotes_CallsFailAsync()
    {
        // Arrange
        var app = CreateValidApplication();
        app.TailoringNotesMarkdown = null;
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act
        await _sut.ProcessAsync("abc123");

        // Assert
        await _repository.Received(1).UpdateStatusAsync(
            "abc123",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Is<string?>(s => s != null && s.Contains("required fields")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_CallsBothGenerationMethods()
    {
        // Arrange
        var app = CreateValidApplication();
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act — ProcessAsync will call generation then try LibreOffice which will fail,
        // but we only care that generation was called.
        try { await _sut.ProcessAsync("abc123"); } catch { /* LibreOffice will fail in test */ }

        // Assert
        await _generationClient.Received(1).GenerateCvAsync(
            Arg.Is<JobApplication>(a => a.Id == "abc123"),
            Arg.Any<CancellationToken>());
        await _generationClient.Received(1).GenerateCoverLetterAsync(
            Arg.Is<JobApplication>(a => a.Id == "abc123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_GenerationThrowsInvalidOperation_PropagatesException()
    {
        // Arrange
        var app = CreateValidApplication();
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);
        _generationClient.GenerateCvAsync(Arg.Any<JobApplication>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Permanent generation failure"));

        // Act
        var act = () => _sut.ProcessAsync("abc123");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Permanent generation failure*");
    }

    [Fact]
    public async Task ProcessAsync_GenerationThrowsHttpRequestException_PropagatesException()
    {
        // Arrange
        var app = CreateValidApplication();
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);
        _generationClient.GenerateCoverLetterAsync(Arg.Any<JobApplication>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Transient generation failure"));

        // Act
        var act = () => _sut.ProcessAsync("abc123");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Transient generation failure*");
    }

    [Fact]
    public async Task MarkFailedAsync_ApplicationNotFound_ReturnsQuietly()
    {
        // Arrange
        _repository.GetByIdAsync("missing", Arg.Any<CancellationToken>())
            .Returns((JobApplication?)null);

        // Act
        var act = () => _sut.MarkFailedAsync("missing", "some reason");

        // Assert
        await act.Should().NotThrowAsync();
        await _repository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Any<string?>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkFailedAsync_ApplicationFound_UpdatesStatusToFailed()
    {
        // Arrange
        var app = CreateValidApplication();
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act
        await _sut.MarkFailedAsync("abc123", "max retries exceeded");

        // Assert
        await _repository.Received(1).UpdateStatusAsync(
            "abc123",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: "max retries exceeded",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkFailedAsync_PublishesFailedEvent()
    {
        // Arrange
        var app = CreateValidApplication();
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act
        await _sut.MarkFailedAsync("abc123", "timeout");

        // Assert
        await _publisher.Received(1).PublishJobApplicationEventAsync(
            Arg.Is<JobApplicationEvent>(e =>
                e.EventType == "updated" &&
                e.Status == "Failed" &&
                e.CompanyName == "Acme Corp"));
    }

    [Fact]
    public async Task MarkFailedAsync_PublishFails_StillUpdatesStatus()
    {
        // Arrange
        var app = CreateValidApplication();
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);
        _publisher.PublishJobApplicationEventAsync(Arg.Any<JobApplicationEvent>())
            .ThrowsAsync(new Exception("RabbitMQ down"));

        // Act
        await _sut.MarkFailedAsync("abc123", "retry exhausted");

        // Assert — UpdateStatusAsync is called before publish in FailAsync, so it should succeed
        await _repository.Received(1).UpdateStatusAsync(
            "abc123",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: "retry exhausted",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_FailAsync_PublishesFailedEvent()
    {
        // Arrange — missing headline triggers FailAsync
        var app = CreateValidApplication();
        app.Headline = null;
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);

        // Act
        await _sut.ProcessAsync("abc123");

        // Assert
        await _publisher.Received(1).PublishJobApplicationEventAsync(
            Arg.Is<JobApplicationEvent>(e =>
                e.EventType == "updated" &&
                e.Status == "Failed"));
    }

    [Fact]
    public async Task ProcessAsync_FailAsync_PublishFails_StillUpdatesStatus()
    {
        // Arrange — missing headline triggers FailAsync, publisher throws
        var app = CreateValidApplication();
        app.Headline = null;
        _repository.GetByIdAsync("abc123", Arg.Any<CancellationToken>()).Returns(app);
        _publisher.PublishJobApplicationEventAsync(Arg.Any<JobApplicationEvent>())
            .ThrowsAsync(new Exception("RabbitMQ down"));

        // Act
        await _sut.ProcessAsync("abc123");

        // Assert — status update should still have been called
        await _repository.Received(1).UpdateStatusAsync(
            "abc123",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Is<string?>(s => s != null && s.Contains("required fields")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ApplicationNotFound_SetsErrorDetailsMessage()
    {
        // Arrange
        _repository.GetByIdAsync("missing-id", Arg.Any<CancellationToken>())
            .Returns((JobApplication?)null);

        // Act
        await _sut.ProcessAsync("missing-id");

        // Assert
        await _repository.Received(1).UpdateStatusAsync(
            "missing-id",
            "Failed",
            commitUrl: Arg.Any<string?>(),
            errorDetails: Arg.Is<string?>(s => s != null && s.Contains("Application record not found")),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
