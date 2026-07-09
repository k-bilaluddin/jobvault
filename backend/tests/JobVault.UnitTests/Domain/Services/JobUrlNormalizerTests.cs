using JobVault.Domain.Services;

namespace JobVault.UnitTests.Domain.Services;

public class JobUrlNormalizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    public void Normalize_InvalidOrMissingInput_ReturnsNull(string? input)
    {
        JobUrlNormalizer.Normalize(input).Should().BeNull();
    }

    [Fact]
    public void Normalize_StripsQueryStringAndFragment()
    {
        var result = JobUrlNormalizer.Normalize("https://example.com/jobs/123?utm_source=linkedin&ref=abc#section");

        result.Should().Be("https://example.com/jobs/123");
    }

    [Fact]
    public void Normalize_StripsTrailingSlash()
    {
        var result = JobUrlNormalizer.Normalize("https://example.com/jobs/123/");

        result.Should().Be("https://example.com/jobs/123");
    }

    [Fact]
    public void Normalize_LowercasesSchemeAndHost()
    {
        var result = JobUrlNormalizer.Normalize("HTTPS://Example.COM/jobs/123");

        result.Should().Be("https://example.com/jobs/123");
    }

    [Fact]
    public void Normalize_LinkedInPlainIdUrl_ProducesCanonicalForm()
    {
        var result = JobUrlNormalizer.Normalize(
            "https://www.linkedin.com/jobs/view/4428814755/?trk=flagship3_search_srp_jobs");

        result.Should().Be("https://www.linkedin.com/jobs/view/4428814755");
    }

    [Fact]
    public void Normalize_LinkedInSlugPrefixedUrl_MatchesPlainIdUrl()
    {
        var slugUrl = JobUrlNormalizer.Normalize(
            "https://www.linkedin.com/jobs/view/senior-backend-engineer-at-bliq-4428814755/");
        var plainUrl = JobUrlNormalizer.Normalize(
            "https://www.linkedin.com/jobs/view/4428814755/?trk=flagship3_search_srp_jobs");

        slugUrl.Should().Be(plainUrl);
        slugUrl.Should().Be("https://www.linkedin.com/jobs/view/4428814755");
    }

    [Fact]
    public void Normalize_GreenhouseUrl_StripsQueryAndKeepsBoardSlugAndId()
    {
        var result = JobUrlNormalizer.Normalize(
            "https://job-boards.eu.greenhouse.io/linkedinjobs/jobs/4917402101?gh_sr=abc123");

        result.Should().Be("https://job-boards.eu.greenhouse.io/linkedinjobs/jobs/4917402101");
    }

    [Fact]
    public void Normalize_UnknownDomain_FallsBackToGenericRule()
    {
        var result = JobUrlNormalizer.Normalize("https://careers.example.com/postings/backend-engineer/?src=email");

        result.Should().Be("https://careers.example.com/postings/backend-engineer");
    }
}
