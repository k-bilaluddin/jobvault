namespace JobVault.Contracts.Requests;

public class UpdatePendingJobRequest
{
    public string? Url { get; set; }
    public string? Status { get; set; }
}
