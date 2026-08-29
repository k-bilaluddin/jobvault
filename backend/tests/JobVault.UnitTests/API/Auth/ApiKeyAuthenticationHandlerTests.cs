using System.Text.Encodings.Web;
using JobVault.API.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobVault.UnitTests.API.Auth;

public class ApiKeyAuthenticationHandlerTests
{
    private static async Task<ApiKeyAuthenticationHandler> CreateHandlerAsync(
        string? configuredKey, string? headerValue)
    {
        var configData = new Dictionary<string, string?> { ["Ingestion:ApiKey"] = configuredKey };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        var optionsMonitor = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.CurrentValue.Returns(new AuthenticationSchemeOptions());
        optionsMonitor.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());

        var handler = new ApiKeyAuthenticationHandler(
            optionsMonitor,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            configuration);

        var httpContext = new DefaultHttpContext();
        if (headerValue is not null)
            httpContext.Request.Headers[ApiKeyValidator.HeaderName] = headerValue;

        var scheme = new AuthenticationScheme(AuthSchemes.ApiKey, AuthSchemes.ApiKey, typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, httpContext);
        return handler;
    }

    [Fact]
    public async Task AuthenticateAsync_ValidKey_Succeeds()
    {
        var handler = await CreateHandlerAsync("secret123", "secret123");

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_MissingHeader_ReturnsNoResult()
    {
        var handler = await CreateHandlerAsync("secret123", headerValue: null);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.None.Should().BeTrue(); // NoResult(), not Fail — lets the JWT scheme be the deciding one
    }

    [Fact]
    public async Task AuthenticateAsync_WrongKey_Fails()
    {
        var handler = await CreateHandlerAsync("secret123", "wrong");

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_NotConfigured_Fails()
    {
        var handler = await CreateHandlerAsync(configuredKey: null, headerValue: "anything");

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }
}
