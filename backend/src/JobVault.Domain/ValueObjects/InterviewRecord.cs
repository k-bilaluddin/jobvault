namespace JobVault.Domain.ValueObjects;

public class InterviewRecord
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Type { get; set; } = "Phone";
    public string Notes { get; set; } = string.Empty;
    public string Outcome { get; set; } = "Pending";
}
