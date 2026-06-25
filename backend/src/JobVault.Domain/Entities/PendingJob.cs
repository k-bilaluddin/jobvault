namespace JobVault.Domain.Entities;

public class PendingJob
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
