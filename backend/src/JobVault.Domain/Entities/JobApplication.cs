namespace JobVault.Domain.Entities;

public class JobApplication
{
    public string? Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobUrl { get; set; } = string.Empty;
    public string WorkMode { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public int? SalaryMin { get; set; }
    public int? SalaryMax { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string SalaryPeriod { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Async ingestion pipeline — populated by API, consumed and cleared by Worker
    public string? CvDocxBase64 { get; set; }
    public string? CoverLetterDocxBase64 { get; set; }
    public string? CompatibilityReportMarkdown { get; set; }
    public string? TailoringNotesMarkdown { get; set; }

    // Set by Worker on completion
    public string? CommitUrl { get; set; }
    public string? ErrorDetails { get; set; }
}