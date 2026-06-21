namespace JobVault.Contracts.Requests;

public record UpdateStageRequest(string Stage);
public record UpdateNotesRequest(string Notes);
public record AddInterviewRequest(string Date, string Type, string Notes, string Outcome);
