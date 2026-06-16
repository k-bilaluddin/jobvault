namespace JobVault.Domain.Entities;

public class AppNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanySlug { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public bool Read { get; set; } = false;
}
