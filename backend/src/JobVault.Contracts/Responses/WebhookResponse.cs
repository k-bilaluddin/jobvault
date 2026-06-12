namespace JobVault.Contracts.Responses;

public class WebhookResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}