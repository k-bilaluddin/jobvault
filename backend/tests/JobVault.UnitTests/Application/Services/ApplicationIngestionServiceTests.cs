using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Application.Services;
using JobVault.Contracts.Events;
using JobVault.Contracts.Requests;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Application.Services;

public class ApplicationIngestionServiceTests
{
    private readonly IJobApplicationRepository _repository;
    private readonly IPendingJobRepository _pendingJobRepository;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<ApplicationIngestionService> _logger;
    private readonly ApplicationIngestionService _sut;

    public ApplicationIngestionServiceTests()
    {
        _repository = Substitute.For<IJobApplicationRepository>();
        _pendingJobRepository = Substitute.For<IPendingJobRepository>();
        _publisher = Substitute.For<IRabbitMqPublisher>();
        _logger = Substitute.For<ILogger<ApplicationIngestionService>>();

        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Success(true, "abc123"));

        _sut = new ApplicationIngestionService(_repository, _pendingJobRepository, _publisher, _logger);
    }

    private static IngestApplicationRequest CreateValidRequest() => new()
    {
        CompanyName = "Acme Corp",
        JobTitle = "Software Engineer",
        Recommendation = "Strong Apply",
        MatchScore = 85,
        Headline = "Experienced developer",
        CompatibilityReportMarkdown = "# Report",
        TailoringNotesMarkdown = "# Notes",
        Roles = new List<RolePayload>
        {
            new() { Id = "calvergy", Bullets = new List<string> { "Led team" } }
        }
    };

    // ─── Validation Tests ────────────────────────────────────────────

    [Fact]
    public async Task IngestAsync_MissingCompanyName_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CompanyName = "";

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("companyName is required");
    }

    [Fact]
    public async Task IngestAsync_MissingJobTitle_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.JobTitle = "";

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("jobTitle is required");
    }

    [Fact]
    public async Task IngestAsync_MissingRecommendation_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Recommendation = "";

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("recommendation is required");
    }

    [Fact]
    public async Task IngestAsync_MatchScoreBelowZero_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MatchScore = -1;

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("matchScore must be between 0 and 100");
    }

    [Fact]
    public async Task IngestAsync_MatchScoreAbove100_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MatchScore = 101;

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("matchScore must be between 0 and 100");
    }

    [Fact]
    public async Task IngestAsync_MatchScoreZero_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MatchScore = 0;

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IngestAsync_MatchScore100_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MatchScore = 100;

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IngestAsync_MissingHeadline_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Headline = "";

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("headline is required");
    }

    [Fact]
    public async Task IngestAsync_MissingCompatibilityReportMarkdown_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CompatibilityReportMarkdown = "";

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("compatibilityReportMarkdown is required");
    }

    [Fact]
    public async Task IngestAsync_MissingTailoringNotesMarkdown_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.TailoringNotesMarkdown = "";

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("tailoringNotesMarkdown is required");
    }

    [Fact]
    public async Task IngestAsync_RoleWithEmptyId_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Roles = new List<RolePayload>
        {
            new() { Id = "", Bullets = new List<string> { "Did stuff" } }
        };

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("each role must have a non-empty id");
    }

    [Fact]
    public async Task IngestAsync_RoleWithInvalidId_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Roles = new List<RolePayload>
        {
            new() { Id = "unknown", Bullets = new List<string> { "Did stuff" } }
        };

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("invalid role id");
    }

    [Theory]
    [InlineData("calvergy")]
    [InlineData("senior_baris")]
    [InlineData("developer_baris")]
    [InlineData("junior_baris")]
    public async Task IngestAsync_ValidRoleId_ReturnsSuccess(string roleId)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Roles = new List<RolePayload>
        {
            new() { Id = roleId, Bullets = new List<string> { "Did stuff" } }
        };

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task IngestAsync_EmptyRolesList_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Roles = new List<RolePayload>();

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ─── Happy Path Tests ────────────────────────────────────────────

    [Fact]
    public async Task IngestAsync_ValidRequest_ReturnsSuccessWithApplicationId()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ApplicationId.Should().Be("abc123");
    }

    [Fact]
    public async Task IngestAsync_ValidRequest_SetsStatusToProcessing()
    {
        // Arrange
        var request = CreateValidRequest();
        JobApplication? capturedApplication = null;
        _repository
            .UpsertApplicationAsync(Arg.Do<JobApplication>(app => capturedApplication = app))
            .Returns(UpsertResult.Success(true, "abc123"));

        // Act
        await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        capturedApplication.Should().NotBeNull();
        capturedApplication!.Status.Should().Be("Processing");
    }

    [Fact]
    public async Task IngestAsync_ValidRequest_MapsGenerationPayloadCorrectly()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Skills = new List<SkillRow>
        {
            new() { Label = "C#", Value = "Expert" }
        };
        request.Strengths = new List<string> { "Leadership" };
        request.Gaps = new List<string> { "Cloud" };

        JobApplication? capturedApplication = null;
        _repository
            .UpsertApplicationAsync(Arg.Do<JobApplication>(app => capturedApplication = app))
            .Returns(UpsertResult.Success(true, "abc123"));

        // Act
        await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        capturedApplication.Should().NotBeNull();
        capturedApplication!.Headline.Should().Be("Experienced developer");
        capturedApplication.Skills.Should().HaveCount(1);
        capturedApplication.Skills[0].Label.Should().Be("C#");
        capturedApplication.Roles.Should().HaveCount(1);
        capturedApplication.Roles[0].Id.Should().Be("calvergy");
        capturedApplication.Strengths.Should().Contain("Leadership");
        capturedApplication.Gaps.Should().Contain("Cloud");
    }

    [Fact]
    public async Task IngestAsync_NullCurrency_DefaultsToEUR()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Currency = null;

        JobApplication? capturedApplication = null;
        _repository
            .UpsertApplicationAsync(Arg.Do<JobApplication>(app => capturedApplication = app))
            .Returns(UpsertResult.Success(true, "abc123"));

        // Act
        await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        capturedApplication.Should().NotBeNull();
        capturedApplication!.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task IngestAsync_NullSalaryPeriod_DefaultsToAnnual()
    {
        // Arrange
        var request = CreateValidRequest();
        request.SalaryPeriod = null;

        JobApplication? capturedApplication = null;
        _repository
            .UpsertApplicationAsync(Arg.Do<JobApplication>(app => capturedApplication = app))
            .Returns(UpsertResult.Success(true, "abc123"));

        // Act
        await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        capturedApplication.Should().NotBeNull();
        capturedApplication!.SalaryPeriod.Should().Be("Annual");
    }

    // ─── Error Handling Tests ────────────────────────────────────────

    [Fact]
    public async Task IngestAsync_RepositoryReturnsFailure_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Failure());

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to persist application");
    }

    [Fact]
    public async Task IngestAsync_RepositoryReturnsSuccessWithNullId_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(UpsertResult.Success(true, null));

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to persist application");
    }

    [Fact]
    public async Task IngestAsync_RabbitMqPublisherThrows_StillReturnsSuccess()
    {
        // Arrange
        var request = CreateValidRequest();
        _publisher
            .PublishJobApplicationEventAsync(Arg.Any<JobApplicationEvent>())
            .Returns(Task.FromException(new InvalidOperationException("RabbitMQ down")));

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ApplicationId.Should().Be("abc123");
    }

    [Fact]
    public async Task IngestAsync_RepositoryThrowsUnexpectedException_ReturnsFailure()
    {
        // Arrange
        var request = CreateValidRequest();
        _repository
            .UpsertApplicationAsync(Arg.Any<JobApplication>())
            .Returns(Task.FromException<UpsertResult>(new InvalidOperationException("DB connection lost")));

        // Act
        var result = await _sut.IngestAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("An unexpected error occurred");
    }
}
