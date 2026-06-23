namespace JobVault.Contracts.Responses;

public class NoteResponse
{
    public int Id { get; init; }
    public string Category { get; init; } = "";
    public string Content { get; init; } = "";
    public string Stage { get; init; } = "";
    public bool Pinned { get; init; }
    public string Created_at { get; init; } = "";
    public string Updated_at { get; init; } = "";
}
