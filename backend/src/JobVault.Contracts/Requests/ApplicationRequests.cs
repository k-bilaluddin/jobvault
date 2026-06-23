namespace JobVault.Contracts.Requests;

public record UpdateStageRequest(string Stage);
public record UpdateNotesRequest(string Notes);
public record AddInterviewRequest(string Date, string Type, string Notes, string Outcome);
public record UpdateInterviewRequest(string? Date, string? Type, string? Notes, string? Outcome);
public record AddNoteRequest(string Category, string Content, bool Pinned = false, string? Stage = null);
public record UpdateNoteRequest(string? Category, string? Content, bool? Pinned);
