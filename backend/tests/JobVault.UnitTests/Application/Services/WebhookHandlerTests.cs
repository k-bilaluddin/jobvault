using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Application.Services;
using JobVault.Contracts.Events;
using JobVault.Contracts.External.GitHub;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Application.Services;

public class WebhookHandlerTests
{
    private readonly IGitHubFileService _gitHubFileService;
    private readonly IMarkdownParserService _markdownParserService;
    private readonly IJobApplicationRepository _repository;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<WebhookHandler> _logger;
    private readonly WebhookHandler _sut;

    public WebhookHandlerTests()
    {
        _gitHubFileService = Substitute.For<IGitHubFileService>();
        _markdownParserService = Substitute.For<IMarkdownParserService>();
        _repository = Substitute.For<IJobApplicationRepository>();
        _rabbitMqPublisher = Substitute.For<IRabbitMqPublisher>();
        _logger = Substitute.For<ILogger<WebhookHandler>>();

        _sut = new WebhookHandler(
            _gitHubFileService,
            _markdownParserService,
            _repository,
            _rabbitMqPublisher,
            _logger);
    }

    private static GitHubWebhookPayload CreatePayloadWithFiles(params string[] addedFiles)
    {
        return new GitHubWebhookPayload
        {
            Ref = "refs/heads/main",
            Repository = new RepositoryInfo { Name = "job-applications" },
            Commits = new List<CommitInfo>
            {
                new()
                {
                    Added = addedFiles.ToList(),
                    Modified = new List<string>()
                }
            }
        };
    }

    private void SetupSuccessfulProcessing(string companyName)
    {
        var application = new JobApplication
        {
            CompanyName = companyName,
            JobTitle = "Software Engineer",
            MatchScore = 85,
            Recommendation = "Strong Apply"
        };

        _gitHubFileService
            .GetFileContentAsync($"{companyName}/compatibility-report.md")
            .Returns("```json\n{}\n```");

        _markdownParserService
            .ExtractJobApplicationAsync(Arg.Any<string>())
            .Returns(application);

        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Success(true, "id123"));
    }

    // ─── Tests ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NullCommits_ReturnsSuccessNoCommitsToProcess()
    {
        // Arrange
        var payload = new GitHubWebhookPayload
        {
            Ref = "refs/heads/main",
            Repository = new RepositoryInfo { Name = "repo" },
            Commits = null!
        };

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("No commits to process");
    }

    [Fact]
    public async Task HandleAsync_EmptyCommits_ReturnsSuccessNoCommitsToProcess()
    {
        // Arrange
        var payload = new GitHubWebhookPayload
        {
            Ref = "refs/heads/main",
            Repository = new RepositoryInfo { Name = "repo" },
            Commits = new List<CommitInfo>()
        };

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("No commits to process");
    }

    [Fact]
    public async Task HandleAsync_CommitsWithNoFiles_ReturnsSuccessNoFilesToProcess()
    {
        // Arrange
        var payload = new GitHubWebhookPayload
        {
            Ref = "refs/heads/main",
            Repository = new RepositoryInfo { Name = "repo" },
            Commits = new List<CommitInfo>
            {
                new() { Added = new List<string>(), Modified = new List<string>() }
            }
        };

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("No files to process");
    }

    [Fact]
    public async Task HandleAsync_FilesWithNoSlash_TreatsFilenameAsCompanyAndFailsProcessing()
    {
        // Arrange
        // A file with no slash means the whole filename becomes the "company name".
        // ProcessCompanyApplicationAsync will attempt to fetch "readme.md/compatibility-report.md"
        // which returns null, so processing fails and processedCount stays 0.
        var payload = CreatePayloadWithFiles("readme.md");

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ValidAddedFile_FetchesParsesAndUpserts()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        SetupSuccessfulProcessing("Acme Corp");

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _gitHubFileService.Received(1).GetFileContentAsync("Acme Corp/compatibility-report.md");
        await _markdownParserService.Received(1).ExtractJobApplicationAsync(Arg.Any<string>());
        await _repository.Received(1).UpsertApplicationAsync(Arg.Any<JobApplication>());
    }

    [Fact]
    public async Task HandleAsync_ModifiedFile_AlsoTriggersProcessing()
    {
        // Arrange
        var payload = new GitHubWebhookPayload
        {
            Ref = "refs/heads/main",
            Repository = new RepositoryInfo { Name = "repo" },
            Commits = new List<CommitInfo>
            {
                new()
                {
                    Added = new List<string>(),
                    Modified = new List<string> { "Acme Corp/compatibility-report.md" }
                }
            }
        };
        SetupSuccessfulProcessing("Acme Corp");

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _gitHubFileService.Received(1).GetFileContentAsync("Acme Corp/compatibility-report.md");
    }

    [Fact]
    public async Task HandleAsync_GitHubFetchReturnsNull_ReturnsFalseForCompany()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        _gitHubFileService
            .GetFileContentAsync(Arg.Any<string>())
            .Returns((string?)null);

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().UpsertApplicationAsync(Arg.Any<JobApplication>());
    }

    [Fact]
    public async Task HandleAsync_ParserReturnsNull_ReturnsFalseForCompany()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        _gitHubFileService
            .GetFileContentAsync(Arg.Any<string>())
            .Returns("some markdown");
        _markdownParserService
            .ExtractJobApplicationAsync(Arg.Any<string>())
            .Returns((JobApplication?)null);

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceive().UpsertApplicationAsync(Arg.Any<JobApplication>());
    }

    [Fact]
    public async Task HandleAsync_UpsertFails_ReturnsFalseForCompany()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        _gitHubFileService
            .GetFileContentAsync(Arg.Any<string>())
            .Returns("some markdown");
        _markdownParserService
            .ExtractJobApplicationAsync(Arg.Any<string>())
            .Returns(new JobApplication { CompanyName = "Acme Corp" });
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Failure());

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_NewDocument_PublishesEventWithCreatedType()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        var application = new JobApplication
        {
            CompanyName = "Acme Corp",
            JobTitle = "Engineer",
            MatchScore = 90,
            Recommendation = "Apply"
        };

        _gitHubFileService
            .GetFileContentAsync(Arg.Any<string>())
            .Returns("markdown content");
        _markdownParserService
            .ExtractJobApplicationAsync(Arg.Any<string>())
            .Returns(application);
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Success(true, "id123"));

        // Act
        await _sut.HandleAsync(payload);

        // Assert
        await _rabbitMqPublisher.Received(1).PublishJobApplicationEventAsync(
            Arg.Is<JobApplicationEvent>(e => e.EventType == "created"));
    }

    [Fact]
    public async Task HandleAsync_ExistingDocument_PublishesEventWithUpdatedType()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        var application = new JobApplication
        {
            CompanyName = "Acme Corp",
            JobTitle = "Engineer",
            MatchScore = 90,
            Recommendation = "Apply"
        };

        _gitHubFileService
            .GetFileContentAsync(Arg.Any<string>())
            .Returns("markdown content");
        _markdownParserService
            .ExtractJobApplicationAsync(Arg.Any<string>())
            .Returns(application);
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Success(false, "id123"));

        // Act
        await _sut.HandleAsync(payload);

        // Assert
        await _rabbitMqPublisher.Received(1).PublishJobApplicationEventAsync(
            Arg.Is<JobApplicationEvent>(e => e.EventType == "updated"));
    }

    [Fact]
    public async Task HandleAsync_RabbitMqThrows_StillReturnsTrueForCompany()
    {
        // Arrange
        var payload = CreatePayloadWithFiles("Acme Corp/compatibility-report.md");
        var application = new JobApplication
        {
            CompanyName = "Acme Corp",
            JobTitle = "Engineer"
        };

        _gitHubFileService
            .GetFileContentAsync(Arg.Any<string>())
            .Returns("markdown content");
        _markdownParserService
            .ExtractJobApplicationAsync(Arg.Any<string>())
            .Returns(application);
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Success(true, "id123"));
        _rabbitMqPublisher
            .PublishJobApplicationEventAsync(Arg.Any<JobApplicationEvent>())
            .Returns(Task.FromException(new InvalidOperationException("RabbitMQ down")));

        // Act
        var result = await _sut.HandleAsync(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
