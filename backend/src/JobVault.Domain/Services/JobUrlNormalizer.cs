using System.Text.RegularExpressions;

namespace JobVault.Domain.Services;

/// <summary>
/// Canonicalizes job posting URLs so the same posting reached via different links
/// (tracking params, slug variants, trailing slashes) compares equal.
/// </summary>
public static partial class JobUrlNormalizer
{
    public static string? Normalize(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.ToLowerInvariant();
        var path = NormalizePath(host, uri.AbsolutePath);

        return $"{uri.Scheme.ToLowerInvariant()}://{host}{path}";
    }

    private static string NormalizePath(string host, string path)
    {
        if (host.EndsWith("linkedin.com", StringComparison.Ordinal))
        {
            var match = LinkedInJobIdPattern().Match(path);
            if (match.Success)
                return $"/jobs/view/{match.Groups[1].Value}";
        }

        if (host.EndsWith("greenhouse.io", StringComparison.Ordinal))
        {
            var match = GreenhouseJobIdPattern().Match(path);
            if (match.Success)
                return $"{match.Groups["prefix"].Value}/{match.Groups["id"].Value}";
        }

        return path.Length > 1 && path.EndsWith('/') ? path[..^1] : path;
    }

    // Matches "/jobs/view/4428814755" or "/jobs/view/senior-backend-engineer-at-bliq-4428814755",
    // with or without a trailing slash — captures the trailing numeric id either way.
    [GeneratedRegex(@"/jobs/view/(?:[a-z0-9-]*-)?(\d+)/?$", RegexOptions.IgnoreCase)]
    private static partial Regex LinkedInJobIdPattern();

    // Matches "/{board-slug}/jobs/4917402101" regardless of subdomain (boards.greenhouse.io,
    // job-boards.eu.greenhouse.io, etc.) — captures the board-slug prefix and the numeric id.
    [GeneratedRegex(@"^(?<prefix>/[^/]+/jobs)/(?<id>\d+)/?$", RegexOptions.IgnoreCase)]
    private static partial Regex GreenhouseJobIdPattern();
}
