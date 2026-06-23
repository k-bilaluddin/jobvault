using JobVault.Domain.ValueObjects;

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

    // Generation payload — populated by API, consumed by Worker to call the generation service
    public string? JdSource { get; set; }
    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public List<SkillRow> Skills { get; set; } = [];
    public List<RolePayload> Roles { get; set; } = [];
    public string? Recipient { get; set; }
    public List<string> CoverLetterParagraphs { get; set; } = [];
    public List<string> Strengths { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
    public string? TailoringNotes { get; set; }

    // Full markdown reports committed as-is to GitHub
    public string? CompatibilityReportMarkdown { get; set; }
    public string? TailoringNotesMarkdown { get; set; }

    // Set by Worker on completion
    public string? CommitUrl { get; set; }
    public string? ErrorDetails { get; set; }

    // Tracking fields (migrated from tracker_data.json)
    public string Stage { get; set; } = string.Empty;
    public bool Applied { get; set; }
    public DateTime? AppliedDate { get; set; }
    public string PersonalNotes { get; set; } = string.Empty;
    public List<InterviewRecord> Interviews { get; set; } = [];
    public List<ApplicationNote> Notes { get; set; } = [];
    public SalaryInfo Salary { get; set; } = new();
    public RecruiterInfo Recruiter { get; set; } = new();
    public DateTime? FollowUpDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsHistorical { get; set; }
}