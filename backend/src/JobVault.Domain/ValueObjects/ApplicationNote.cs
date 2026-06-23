namespace JobVault.Domain.ValueObjects;

public class ApplicationNote
{
    public int Id { get; set; }
    public string Category { get; set; } = "General";
    public string Content { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public bool Pinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
