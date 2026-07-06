namespace JobVault.Infrastructure.Vault;

/// <summary>
/// Builds the GitHub vault folder name for an application. Format is "{companyName}-{id}" —
/// the full application id (not truncated) so the folder name doubles as a direct lookup key,
/// and a company can have more than one application without its files colliding. See issue #104.
/// </summary>
public static class VaultPathBuilder
{
    public static string BuildFolderName(string companyName, string id) => $"{companyName}-{id}";
}
