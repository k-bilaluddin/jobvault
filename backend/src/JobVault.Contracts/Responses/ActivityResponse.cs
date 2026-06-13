namespace JobVault.Contracts.Responses;

public class ActivityResponse
{
    public string? Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty; // "Created", "Updated", "StatusChanged"
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime Timestamp { get; set; }
}