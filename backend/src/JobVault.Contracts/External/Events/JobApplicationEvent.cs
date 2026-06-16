namespace JobVault.Contracts.Events;

public class JobApplicationEvent
{
    public string? ApplicationId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}