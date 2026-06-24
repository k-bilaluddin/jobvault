namespace JobVault.Contracts.Requests;

public record UpdateStageRequest(string Stage);
public record UpdateNotesRequest(string Notes);
public record AddInterviewRequest(string Date, string Type, string Notes, string Outcome);
public record UpdateInterviewRequest(string? Date, string? Type, string? Notes, string? Outcome);
public record AddNoteRequest(string Category, string Content, bool Pinned = false, string? Stage = null);
public record UpdateNoteRequest(string? Category, string? Content, bool? Pinned);

public class UpdateContentRequest
{
    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public List<Domain.ValueObjects.SkillRow>? Skills { get; set; }
    public List<Domain.ValueObjects.RolePayload>? Roles { get; set; }
    public string? Recipient { get; set; }
    public List<string>? CoverLetterParagraphs { get; set; }
    public List<string>? Strengths { get; set; }
    public List<string>? Gaps { get; set; }
}
