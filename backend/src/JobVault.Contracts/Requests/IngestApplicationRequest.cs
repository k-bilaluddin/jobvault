namespace JobVault.Contracts.Requests;

public class IngestApplicationRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? JobUrl { get; set; }
    public string? WorkMode { get; set; }
    public string? EmploymentType { get; set; }
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string? Currency { get; set; }
    public string? SalaryPeriod { get; set; }
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string CvDocxBase64 { get; set; } = string.Empty;
    public string CoverLetterDocxBase64 { get; set; } = string.Empty;
    public string CompatibilityReportMarkdown { get; set; } = string.Empty;
    public string TailoringNotesMarkdown { get; set; } = string.Empty;
}
