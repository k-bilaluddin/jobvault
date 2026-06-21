using System.Text.RegularExpressions;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;

namespace JobVault.Application.Services;

public class ApplicationQueryService : IApplicationQueryService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IVaultFileService _vaultFileService;
    private readonly IMarkdownRenderService _markdownRenderService;

    public ApplicationQueryService(
        IJobApplicationRepository repository,
        IVaultFileService vaultFileService,
        IMarkdownRenderService markdownRenderService)
    {
        _repository = repository;
        _vaultFileService = vaultFileService;
        _markdownRenderService = markdownRenderService;
    }

    public async Task<IReadOnlyList<ApplicationResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        return applications
            .Where(a => !a.IsHistorical)
            .Select(a =>
            {
                var stage = a.Status is "Processing" or "Failed"
                    ? a.Status
                    : string.IsNullOrEmpty(a.Stage) ? "Ready to Apply" : a.Stage;

                var (hasCvPdf, hasLetterPdf, hasReport, hasNotes) = _vaultFileService.CheckFiles(a.CompanyName);

                return new ApplicationResponse
                {
                    Name = a.CompanyName,
                    Synced_at = a.UpdatedAt.ToString("o"),
                    Has_report = !string.IsNullOrEmpty(a.CompatibilityReportMarkdown) || hasReport,
                    Has_notes = !string.IsNullOrEmpty(a.TailoringNotesMarkdown) || hasNotes,
                    Has_cv_pdf = !string.IsNullOrEmpty(a.CommitUrl) || hasCvPdf,
                    Has_letter_pdf = !string.IsNullOrEmpty(a.CommitUrl) || hasLetterPdf,
                    Match_pct = a.MatchScore > 0 ? a.MatchScore : null,
                    Recommend = a.Recommendation,
                    Job_url = a.JobUrl,
                    Role = a.JobTitle,
                    Applied = a.Applied,
                    Applied_date = a.AppliedDate?.ToString("yyyy-MM-dd") ?? "",
                    Stage = stage,
                    Personal_notes = a.PersonalNotes,
                    Interviews = a.Interviews.Select(i => new InterviewResponse
                    {
                        Id = i.Id, Date = i.Date, Type = i.Type, Notes = i.Notes, Outcome = i.Outcome,
                    }).ToList(),
                    Salary = new SalaryResponse
                    {
                        Advertised = a.Salary.Advertised, Target = a.Salary.Target,
                        Discussed = a.Salary.Discussed, Offered = a.Salary.Offered,
                    },
                    Recruiter = new RecruiterResponse
                    {
                        Name = a.Recruiter.Name, Email = a.Recruiter.Email, Linkedin = a.Recruiter.LinkedIn,
                    },
                    Follow_up_date = a.FollowUpDate?.ToString("yyyy-MM-dd") ?? "",
                    Source = a.Source,
                };
            })
            .ToList();
    }

    public async Task<string?> GetReportHtmlAsync(string companyName, CancellationToken cancellationToken = default)
    {
        var app = await _repository.GetByCompanyNameAsync(companyName, cancellationToken);
        if (app == null) return null;

        var markdown = app.CompatibilityReportMarkdown;

        if (string.IsNullOrEmpty(markdown))
            markdown = _vaultFileService.ReadMarkdown(companyName, ["compatibility-report", "compatibility_report", "report"]);

        if (string.IsNullOrEmpty(markdown)) return null;

        return _markdownRenderService.RenderToHtml(markdown);
    }

    public async Task<string?> GetNotesHtmlAsync(string companyName, CancellationToken cancellationToken = default)
    {
        var app = await _repository.GetByCompanyNameAsync(companyName, cancellationToken);
        if (app == null) return null;

        var markdown = app.TailoringNotesMarkdown;

        if (string.IsNullOrEmpty(markdown))
            markdown = _vaultFileService.ReadMarkdown(companyName, ["tailoring-notes", "tailoring_notes", "notes"]);

        if (string.IsNullOrEmpty(markdown)) return null;

        return _markdownRenderService.RenderToHtml(markdown);
    }

    private static readonly Regex GapMarkers = new(
        @"\b(gap|missing|no match|partial|not found|lacking|none|weak|limited|no experience|not demonstrated)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task<SkillsGapResponse> GetSkillsGapAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        var skillCounts = new Dictionary<string, (string Skill, int Count, List<string> Companies)>(StringComparer.OrdinalIgnoreCase);
        var reportsScanned = 0;

        foreach (var app in applications.Where(a => !a.IsHistorical))
        {
            // Source 1: structured Gaps array from MongoDB
            foreach (var gap in app.Gaps)
                AddGap(skillCounts, gap, app.CompanyName);

            // Source 2: parse compatibility report markdown for gap markers
            var reportMarkdown = app.CompatibilityReportMarkdown;
            if (string.IsNullOrEmpty(reportMarkdown))
                reportMarkdown = _vaultFileService.ReadMarkdown(app.CompanyName, ["compatibility-report", "compatibility_report", "report"]);

            if (!string.IsNullOrEmpty(reportMarkdown))
            {
                reportsScanned++;
                ExtractGapsFromReport(reportMarkdown, app.CompanyName, skillCounts);
            }
        }

        return new SkillsGapResponse
        {
            Gaps = skillCounts.Values
                .OrderByDescending(x => x.Count)
                .Select(x => new SkillGapEntry { Skill = x.Skill, Count = x.Count, Companies = x.Companies })
                .ToList(),
            Reports_scanned = reportsScanned,
        };
    }

    private static void AddGap(
        Dictionary<string, (string Skill, int Count, List<string> Companies)> skillCounts,
        string skill, string companyName)
    {
        var key = skill.ToLowerInvariant();
        if (!skillCounts.TryGetValue(key, out var entry))
        {
            entry = (skill, 0, []);
            skillCounts[key] = entry;
        }

        if (!entry.Companies.Contains(companyName))
        {
            entry.Count++;
            entry.Companies.Add(companyName);
            skillCounts[key] = entry;
        }
    }

    private static void ExtractGapsFromReport(
        string reportMarkdown, string companyName,
        Dictionary<string, (string Skill, int Count, List<string> Companies)> skillCounts)
    {
        foreach (var line in reportMarkdown.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('|')) continue;

            var cells = trimmed.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length < 2) continue;

            // Skip header/separator rows
            if (Regex.IsMatch(cells[0], @"^[-:]+$") ||
                cells[0].Equals("Category", StringComparison.OrdinalIgnoreCase) ||
                cells[0].Equals("Skill", StringComparison.OrdinalIgnoreCase) ||
                cells[0].Equals("Requirement", StringComparison.OrdinalIgnoreCase) ||
                cells[0] == "#" || cells[0] == "")
                continue;

            var rowText = string.Join(' ', cells);
            if (!GapMarkers.IsMatch(rowText)) continue;

            var skill = cells[0].Trim('*', ' ');
            if (string.IsNullOrEmpty(skill) || skill.Length > 60) continue;

            AddGap(skillCounts, skill, companyName);
        }
    }

    public async Task<HistoricalResponse> GetHistoricalAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        return new HistoricalResponse
        {
            Available = true,
            Historical = applications
                .Where(a => a.IsHistorical)
                .Select(a => new HistoricalEntry
                {
                    Name = a.CompanyName,
                    Applied_date = a.AppliedDate?.ToString("yyyy-MM-dd") ?? "",
                    Stage = string.IsNullOrEmpty(a.Stage) ? "Applied" : a.Stage,
                    Source = a.Source,
                })
                .ToList(),
            Current = applications
                .Where(a => !a.IsHistorical && a.Applied)
                .Select(a => new CurrentEntry
                {
                    Name = a.CompanyName,
                    Applied_date = a.AppliedDate?.ToString("yyyy-MM-dd") ?? "",
                    Stage = string.IsNullOrEmpty(a.Stage) ? "Applied" : a.Stage,
                    Source = a.Source,
                })
                .ToList(),
            Scanned_at = DateTime.UtcNow.ToString("o"),
        };
    }

    public Task<bool> UpdateStageAsync(string companyName, string stage, CancellationToken cancellationToken = default)
        => _repository.UpdateStageAsync(companyName, stage, cancellationToken);

    public Task<bool> UpdatePersonalNotesAsync(string companyName, string notes, CancellationToken cancellationToken = default)
        => _repository.UpdatePersonalNotesAsync(companyName, notes, cancellationToken);

    public Task<JobApplication?> AddInterviewAsync(string companyName, InterviewRecord interview, CancellationToken cancellationToken = default)
        => _repository.AddInterviewAsync(companyName, interview, cancellationToken);

    public Task<bool> DeleteInterviewAsync(string companyName, int index, CancellationToken cancellationToken = default)
        => _repository.DeleteInterviewAsync(companyName, index, cancellationToken);
}
