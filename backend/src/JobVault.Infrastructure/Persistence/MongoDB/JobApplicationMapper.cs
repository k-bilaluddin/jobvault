using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;

namespace JobVault.Infrastructure.Persistence.MongoDB;

internal static class JobApplicationMapper
{
    public static JobApplication ToDomain(JobApplicationDocument doc) => new()
    {
        Id = doc.Id,
        CompanyName = doc.CompanyName ?? "",
        JobTitle = doc.JobTitle ?? "",
        Location = doc.Location ?? "",
        JobUrl = doc.JobUrl ?? "",
        WorkMode = doc.WorkMode ?? "",
        EmploymentType = doc.EmploymentType ?? "",
        SalaryMin = doc.SalaryMin,
        SalaryMax = doc.SalaryMax,
        Currency = doc.Currency ?? "EUR",
        SalaryPeriod = doc.SalaryPeriod ?? "",
        MatchScore = doc.MatchScore ?? 0,
        Recommendation = doc.Recommendation ?? "",
        Status = doc.Status ?? "",
        CreatedAt = doc.CreatedAt ?? default,
        UpdatedAt = doc.UpdatedAt ?? default,
        JdSource = doc.JdSource,
        Headline = doc.Headline,
        Summary = doc.Summary,
        Skills = doc.Skills?.Select(s => new SkillRow
        {
            Label = s.Label ?? "",
            Value = s.Value ?? "",
        }).ToList() ?? [],
        Roles = doc.Roles?.Select(r => new RolePayload
        {
            Id = r.Id ?? "",
            Bullets = r.Bullets ?? [],
        }).ToList() ?? [],
        Recipient = doc.Recipient,
        CoverLetterParagraphs = doc.CoverLetterParagraphs ?? [],
        Strengths = doc.Strengths ?? [],
        Gaps = doc.Gaps ?? [],
        TailoringNotes = doc.TailoringNotes,
        CompatibilityReportMarkdown = doc.CompatibilityReportMarkdown,
        TailoringNotesMarkdown = doc.TailoringNotesMarkdown,
        CommitUrl = doc.CommitUrl,
        ErrorDetails = doc.ErrorDetails,
        Stage = doc.Stage ?? "",
        Applied = doc.Applied ?? false,
        AppliedDate = doc.AppliedDate,
        PersonalNotes = doc.PersonalNotes ?? "",
        Interviews = doc.Interviews?.Select(i => new InterviewRecord
        {
            Id = i.Id ?? 0,
            Date = i.Date ?? "",
            Type = i.Type ?? "Phone",
            Notes = i.Notes ?? "",
            Outcome = i.Outcome ?? "Pending",
        }).ToList() ?? [],
        Salary = doc.Salary != null
            ? new SalaryInfo
            {
                Advertised = doc.Salary.Advertised ?? "",
                Target = doc.Salary.Target ?? "",
                Discussed = doc.Salary.Discussed ?? "",
                Offered = doc.Salary.Offered ?? "",
            }
            : new SalaryInfo(),
        Recruiter = doc.Recruiter != null
            ? new RecruiterInfo
            {
                Name = doc.Recruiter.Name ?? "",
                Email = doc.Recruiter.Email ?? "",
                LinkedIn = doc.Recruiter.LinkedIn ?? "",
            }
            : new RecruiterInfo(),
        FollowUpDate = doc.FollowUpDate,
        Source = doc.Source ?? "",
        IsHistorical = doc.IsHistorical ?? false,
    };

    public static JobApplicationDocument ToDocument(JobApplication entity, string? existingId = null) => new()
    {
        Id = existingId ?? entity.Id ?? "",
        CompanyName = entity.CompanyName,
        JobTitle = entity.JobTitle,
        Location = entity.Location,
        JobUrl = entity.JobUrl,
        WorkMode = entity.WorkMode,
        EmploymentType = entity.EmploymentType,
        SalaryMin = entity.SalaryMin,
        SalaryMax = entity.SalaryMax,
        Currency = entity.Currency.Length > 0 ? entity.Currency : "EUR",
        SalaryPeriod = entity.SalaryPeriod,
        MatchScore = entity.MatchScore,
        Recommendation = entity.Recommendation,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        JdSource = entity.JdSource,
        Headline = entity.Headline,
        Summary = entity.Summary,
        Skills = entity.Skills.Select(s => new SkillRowDocument { Label = s.Label, Value = s.Value }).ToList(),
        Roles = entity.Roles.Select(r => new RolePayloadDocument { Id = r.Id, Bullets = r.Bullets }).ToList(),
        Recipient = entity.Recipient,
        CoverLetterParagraphs = entity.CoverLetterParagraphs,
        Strengths = entity.Strengths,
        Gaps = entity.Gaps,
        TailoringNotes = entity.TailoringNotes,
        CompatibilityReportMarkdown = entity.CompatibilityReportMarkdown,
        TailoringNotesMarkdown = entity.TailoringNotesMarkdown,
        CommitUrl = entity.CommitUrl,
        ErrorDetails = entity.ErrorDetails,
        Stage = entity.Stage.Length > 0 ? entity.Stage : "Ready to Apply",
        Applied = entity.Applied,
        AppliedDate = entity.AppliedDate,
        PersonalNotes = entity.PersonalNotes,
        Interviews = entity.Interviews.Select(i => new InterviewDocument
        {
            Id = i.Id,
            Date = i.Date,
            Type = i.Type,
            Notes = i.Notes,
            Outcome = i.Outcome,
        }).ToList(),
        Salary = new SalaryDocument
        {
            Advertised = entity.Salary.Advertised,
            Target = entity.Salary.Target,
            Discussed = entity.Salary.Discussed,
            Offered = entity.Salary.Offered,
        },
        Recruiter = new RecruiterDocument
        {
            Name = entity.Recruiter.Name,
            Email = entity.Recruiter.Email,
            LinkedIn = entity.Recruiter.LinkedIn,
        },
        FollowUpDate = entity.FollowUpDate,
        Source = entity.Source,
        IsHistorical = entity.IsHistorical,
    };
}
