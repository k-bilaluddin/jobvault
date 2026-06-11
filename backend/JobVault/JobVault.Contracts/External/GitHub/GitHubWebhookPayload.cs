using System.Text.Json.Serialization;

namespace JobVault.Contracts.External.GitHub;

public class GitHubWebhookPayload
{
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty;

    [JsonPropertyName("repository")]
    public RepositoryInfo Repository { get; set; } = new();

    [JsonPropertyName("commits")]
    public List<CommitInfo> Commits { get; set; } = new();
}

public class RepositoryInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class CommitInfo
{
    [JsonPropertyName("added")]
    public List<string> Added { get; set; } = new();

    [JsonPropertyName("modified")]
    public List<string> Modified { get; set; } = new();
}