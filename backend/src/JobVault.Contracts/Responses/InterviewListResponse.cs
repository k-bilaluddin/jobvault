namespace JobVault.Contracts.Responses;

public class InterviewListResponse
{
    public bool Ok { get; init; }
    public List<InterviewResponse> Interviews { get; init; } = [];
}
