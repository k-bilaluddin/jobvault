using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace JobVault.Infrastructure.Persistence.MongoDB;

[BsonIgnoreExtraElements]
internal class JobApplicationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("companyName")]
    public string? CompanyName { get; set; }

    [BsonElement("jobTitle")]
    public string? JobTitle { get; set; }

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("jobUrl")]
    public string? JobUrl { get; set; }

    [BsonElement("workMode")]
    public string? WorkMode { get; set; }

    [BsonElement("employmentType")]
    public string? EmploymentType { get; set; }

    [BsonElement("salaryMin")]
    public int? SalaryMin { get; set; }

    [BsonElement("salaryMax")]
    public int? SalaryMax { get; set; }

    [BsonElement("currency")]
    public string? Currency { get; set; }

    [BsonElement("salaryPeriod")]
    public string? SalaryPeriod { get; set; }

    [BsonElement("matchScore")]
    public int? MatchScore { get; set; }

    [BsonElement("recommendation")]
    public string? Recommendation { get; set; }

    [BsonElement("status")]
    public string? Status { get; set; }

    [BsonElement("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Generation payload
    [BsonElement("jdSource")]
    public string? JdSource { get; set; }

    [BsonElement("headline")]
    public string? Headline { get; set; }

    [BsonElement("summary")]
    public string? Summary { get; set; }

    [BsonElement("skills")]
    public List<SkillRowDocument>? Skills { get; set; }

    [BsonElement("roles")]
    public List<RolePayloadDocument>? Roles { get; set; }

    [BsonElement("recipient")]
    public string? Recipient { get; set; }

    [BsonElement("coverLetterParagraphs")]
    public List<string>? CoverLetterParagraphs { get; set; }

    [BsonElement("strengths")]
    public List<string>? Strengths { get; set; }

    [BsonElement("gaps")]
    public List<string>? Gaps { get; set; }

    [BsonElement("tailoringNotes")]
    public string? TailoringNotes { get; set; }

    [BsonElement("compatibilityReportMarkdown")]
    public string? CompatibilityReportMarkdown { get; set; }

    [BsonElement("tailoringNotesMarkdown")]
    public string? TailoringNotesMarkdown { get; set; }

    [BsonElement("commitUrl")]
    public string? CommitUrl { get; set; }

    [BsonElement("errorDetails")]
    public string? ErrorDetails { get; set; }

    // Tracking fields
    [BsonElement("stage")]
    public string? Stage { get; set; }

    [BsonElement("applied")]
    public bool? Applied { get; set; }

    [BsonElement("appliedDate")]
    public DateTime? AppliedDate { get; set; }

    [BsonElement("personalNotes")]
    public string? PersonalNotes { get; set; }

    [BsonElement("interviews")]
    public List<InterviewDocument>? Interviews { get; set; }

    [BsonElement("salary")]
    public SalaryDocument? Salary { get; set; }

    [BsonElement("recruiter")]
    public RecruiterDocument? Recruiter { get; set; }

    [BsonElement("followUpDate")]
    public DateTime? FollowUpDate { get; set; }

    [BsonElement("source")]
    public string? Source { get; set; }

    [BsonElement("isHistorical")]
    public bool? IsHistorical { get; set; }
}

[BsonIgnoreExtraElements]
internal class SkillRowDocument
{
    [BsonElement("label")]
    public string? Label { get; set; }

    [BsonElement("value")]
    public string? Value { get; set; }
}

[BsonIgnoreExtraElements]
internal class RolePayloadDocument
{
    [BsonElement("id")]
    public string? Id { get; set; }

    [BsonElement("bullets")]
    public List<string>? Bullets { get; set; }
}

[BsonIgnoreExtraElements]
internal class InterviewDocument
{
    [BsonElement("id")]
    public int? Id { get; set; }

    [BsonElement("date")]
    public string? Date { get; set; }

    [BsonElement("type")]
    public string? Type { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("outcome")]
    public string? Outcome { get; set; }
}

[BsonIgnoreExtraElements]
internal class SalaryDocument
{
    [BsonElement("advertised")]
    public string? Advertised { get; set; }

    [BsonElement("target")]
    public string? Target { get; set; }

    [BsonElement("discussed")]
    public string? Discussed { get; set; }

    [BsonElement("offered")]
    public string? Offered { get; set; }
}

[BsonIgnoreExtraElements]
internal class RecruiterDocument
{
    [BsonElement("name")]
    public string? Name { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("linkedin")]
    public string? LinkedIn { get; set; }
}
