using JobVault.API.Controllers;
using JobVault.API.Models.Requests;
using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Application.Models;
using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.API.Controllers;

public class VaultControllerTests
{
    private readonly IFileIngestService _fileIngestService;
    private readonly IApplicationIngestionService _applicationIngestionService;
    private readonly ILogger<VaultController> _logger;
    private readonly VaultController _sut;

    public VaultControllerTests()
    {
        _fileIngestService = Substitute.For<IFileIngestService>();
        _applicationIngestionService = Substitute.For<IApplicationIngestionService>();
        _logger = Substitute.For<ILogger<VaultController>>();
        _sut = new VaultController(_fileIngestService, _applicationIngestionService, _logger);
    }

    // ── IngestApplication (async JSON ingestion) ──

    [Fact]
    public async Task IngestApplication_Success_Returns202WithApplicationId()
    {
        // Arrange
        var request = new IngestApplicationRequest { CompanyName = "Acme", JobTitle = "Dev" };
        var ingestionResult = ApplicationIngestionResult.Success("id123");
        _applicationIngestionService
            .IngestAsync(request, Arg.Any<CancellationToken>())
            .Returns(ingestionResult);

        // Act
        var result = await _sut.IngestApplication(request, CancellationToken.None);

        // Assert
        var accepted = result.Should().BeOfType<AcceptedResult>().Subject;
        var response = accepted.Value.Should().BeOfType<IngestApplicationResponse>().Subject;
        response.ApplicationId.Should().Be("id123");
    }

    [Fact]
    public async Task IngestApplication_ValidationFails_Returns400WithError()
    {
        // Arrange
        var request = new IngestApplicationRequest { CompanyName = "" };
        var ingestionResult = ApplicationIngestionResult.Failure("companyName is required");
        _applicationIngestionService
            .IngestAsync(request, Arg.Any<CancellationToken>())
            .Returns(ingestionResult);

        // Act
        var result = await _sut.IngestApplication(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task IngestApplication_ServiceThrows_Returns500()
    {
        // Arrange
        var request = new IngestApplicationRequest { CompanyName = "Acme" };
        _applicationIngestionService
            .IngestAsync(request, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Boom"));

        // Act
        var result = await _sut.IngestApplication(request, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    // ── Ingest (legacy file upload) ──

    [Fact]
    public async Task Ingest_MissingCompanyName_Returns400()
    {
        // Arrange
        var request = new IngestRequest { CompanyName = "  ", Files = new List<IFormFile>() };

        // Act
        var result = await _sut.Ingest(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Ingest_NoFiles_Returns400()
    {
        // Arrange
        var request = new IngestRequest { CompanyName = "Acme", Files = null! };

        // Act
        var result = await _sut.Ingest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ingest_EmptyFilesList_Returns400()
    {
        // Arrange
        var request = new IngestRequest { CompanyName = "Acme", Files = new List<IFormFile>() };

        // Act
        var result = await _sut.Ingest(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Ingest_ServiceReturnsFailure_Returns500()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.Length.Returns(100);
        formFile.FileName.Returns("resume.pdf");
        formFile.OpenReadStream().Returns(new MemoryStream(new byte[100]));

        var request = new IngestRequest
        {
            CompanyName = "Acme",
            Files = new List<IFormFile> { formFile }
        };

        _fileIngestService
            .IngestAsync("Acme", Arg.Any<IReadOnlyCollection<IngestedFile>>(), Arg.Any<CancellationToken>())
            .Returns(FileIngestResult.Failure("GitHub error"));

        // Act
        var result = await _sut.Ingest(request, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Ingest_ServiceThrows_Returns500()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.Length.Returns(100);
        formFile.FileName.Returns("resume.pdf");
        formFile.OpenReadStream().Returns(new MemoryStream(new byte[100]));

        var request = new IngestRequest
        {
            CompanyName = "Acme",
            Files = new List<IFormFile> { formFile }
        };

        _fileIngestService
            .IngestAsync("Acme", Arg.Any<IReadOnlyCollection<IngestedFile>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Boom"));

        // Act
        var result = await _sut.Ingest(request, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }
}
