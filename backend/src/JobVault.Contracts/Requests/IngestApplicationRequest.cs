using JobVault.Domain.ValueObjects;

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

    // Generation payload — forwarded to the document generation service
    public string? JdSource { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<SkillRow> Skills { get; set; } = [];
    public List<RolePayload> Roles { get; set; } = [];
    public string? Recipient { get; set; }
    public List<string> CoverLetterParagraphs { get; set; } = [];
    public List<string> Strengths { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
    public string? TailoringNotes { get; set; }

    // Optional: links this ingestion to a pending job queue entry
    public string? JobId { get; set; }

    // Full markdown reports committed as-is to GitHub
    public string CompatibilityReportMarkdown { get; set; } = string.Empty;
    public string TailoringNotesMarkdown { get; set; } = string.Empty;
}
