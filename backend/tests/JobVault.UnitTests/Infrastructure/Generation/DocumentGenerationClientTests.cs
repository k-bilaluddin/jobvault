using System.Net;
using System.Net.Http;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;
using JobVault.Infrastructure.Generation;
using Microsoft.Extensions.Logging;

namespace JobVault.UnitTests.Infrastructure.Generation;

public class DocumentGenerationClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly ILogger<DocumentGenerationClient> _logger = Substitute.For<ILogger<DocumentGenerationClient>>();
    private readonly DocumentGenerationClient _sut;

    public DocumentGenerationClientTests()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://test-generation:3000"),
        };
        _sut = new DocumentGenerationClient(httpClient, _logger);
    }

    private static JobApplication CreateTestApplication() => new()
    {
        CompanyName = "TestCorp",
        JobTitle = "Developer",
        MatchScore = 75,
        Headline = "Test headline",
        Summary = "Test summary",
        Skills = [new() { Label = "C#", Value = "Expert" }],
        Roles = [new() { Id = "calvergy", Bullets = ["Led team"] }],
        CoverLetterParagraphs = ["Para 1"],
        Strengths = ["Strong coding"],
        Gaps = ["No cloud exp"],
    };

    [Fact]
    public async Task GenerateCvAsync_Success_ReturnsByteArray()
    {
        // Arrange
        var expected = new byte[] { 10, 20, 30 };
        _handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expected),
        };

        // Act
        var result = await _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GenerateCoverLetterAsync_Success_ReturnsByteArray()
    {
        // Arrange
        var expected = new byte[] { 40, 50, 60 };
        _handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(expected),
        };

        // Act
        var result = await _sut.GenerateCoverLetterAsync(CreateTestApplication());

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GenerateCvAsync_PostsToCorrectEndpoint()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1]),
        };

        // Act
        await _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        _handler.LastRequest.Should().NotBeNull();
        _handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/api/generate-cv");
    }

    [Fact]
    public async Task GenerateCoverLetterAsync_PostsToCorrectEndpoint()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1]),
        };

        // Act
        await _sut.GenerateCoverLetterAsync(CreateTestApplication());

        // Assert
        _handler.LastRequest.Should().NotBeNull();
        _handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/api/generate-cover-letter");
    }

    [Fact]
    public async Task GenerateCvAsync_4xxResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request body"),
        };

        // Act
        var act = () => _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateCvAsync_422Response_ThrowsInvalidOperationException()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("Validation error"),
        };

        // Act
        var act = () => _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateCvAsync_5xxResponse_ThrowsHttpRequestException()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error"),
        };

        // Act
        var act = () => _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GenerateCvAsync_503Response_ThrowsHttpRequestException()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Service unavailable"),
        };

        // Act
        var act = () => _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GenerateCvAsync_4xxResponseBody_IncludedInExceptionMessage()
    {
        // Arrange
        var errorBody = "Missing required field: headline";
        _handler.Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorBody),
        };

        // Act
        var act = () => _sut.GenerateCvAsync(CreateTestApplication());

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage($"*{errorBody}*");
    }

    [Fact]
    public async Task GenerateCvAsync_MapsPayloadCorrectly()
    {
        // Arrange
        _handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1]),
        };
        var application = CreateTestApplication();

        // Act
        await _sut.GenerateCvAsync(application);

        // Assert
        _handler.LastRequest.Should().NotBeNull();
        _handler.LastRequest!.Content.Should().NotBeNull();

        var json = await _handler.LastRequest.Content!.ReadAsStringAsync();

        json.Should().Contain("\"company\":\"TestCorp\"");
        json.Should().Contain("\"role\":\"Developer\"");
        json.Should().Contain("\"compatibilityScore\":75");
        json.Should().Contain("\"headline\":\"Test headline\"");
        json.Should().Contain("\"C#\"");
        json.Should().Contain("\"Expert\"");
        json.Should().Contain("\"calvergy\"");
        json.Should().Contain("\"Led team\"");
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(Response);
        }
    }
}
