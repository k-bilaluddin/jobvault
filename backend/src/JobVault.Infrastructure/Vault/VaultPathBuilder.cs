using System.Text.RegularExpressions;

namespace JobVault.Infrastructure.Vault;

/// <summary>
/// Builds the GitHub vault folder name for an application. Format is "{companyName}-{id}" —
/// the full application id (not truncated) so the folder name doubles as a direct lookup key,
/// and a company can have more than one application without its files colliding. See issue #104.
/// </summary>
public static class VaultPathBuilder
{
    // FileIngestService passes this straight into a Git Trees API path (e.g. "{folderName}/cv.pdf"),
    // where '/' is always a hierarchy separator — never a literal character. A company name like
    // "Confidential AI Startup (via Workfully / jabran farhat)" silently splits into two nested
    // folders instead of one, and the segment before the slash ends in a space, which Windows
    // refuses to check out ("invalid path") on `git pull`. Strip anything that isn't safe as a
    // single path component on both git and Windows: '\ / : * ? " < > |'.
    private static readonly Regex InvalidPathChars = new("[\\\\/:*?\"<>|]", RegexOptions.Compiled);

    public static string BuildFolderName(string companyName, string id) =>
        $"{SanitizeCompanyName(companyName)}-{id}";

    private static string SanitizeCompanyName(string companyName)
    {
        var sanitized = InvalidPathChars.Replace(companyName, " ");
        sanitized = Regex.Replace(sanitized, @"\s+", " ").Trim();

        // Windows also rejects a folder/file name ending in a space or a dot.
        return sanitized.TrimEnd(' ', '.');
    }
}
