namespace JobVault.Contracts.Responses;

public class NotificationResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Body { get; init; } = "";
    public string? CompanyName { get; init; }
    public string? CompanySlug { get; init; }
    public string OccurredAt { get; init; } = "";
    public bool Read { get; init; }
}
