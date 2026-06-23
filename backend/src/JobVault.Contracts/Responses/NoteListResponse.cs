namespace JobVault.Contracts.Responses;

public class NoteListResponse
{
    public bool Ok { get; init; }
    public List<NoteResponse> Notes { get; init; } = [];
}
