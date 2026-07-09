namespace JobVault.Domain.Entities;

public class PendingJob
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? Prompt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Set when this pending job originates from re-queueing an existing application
    // (the "Re-Analyze" flow) — lets ingestion resolve back to that exact application
    // without needing to infer a match by URL or company+title.
    public string? SourceApplicationId { get; set; }
}
