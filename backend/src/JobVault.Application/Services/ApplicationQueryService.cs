using System.Text.RegularExpressions;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace JobVault.Application.Services;

public class ApplicationQueryService : IApplicationQueryService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IVaultFileService _vaultFileService;
    private readonly IMarkdownRenderService _markdownRenderService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IPendingJobService _pendingJobService;
    private readonly ILogger<ApplicationQueryService> _logger;

    public ApplicationQueryService(
        IJobApplicationRepository repository,
        IVaultFileService vaultFileService,
        IMarkdownRenderService markdownRenderService,
        IRabbitMqPublisher publisher,
        IPendingJobService pendingJobService,
        ILogger<ApplicationQueryService> logger)
    {
        _repository = repository;
        _vaultFileService = vaultFileService;
        _markdownRenderService = markdownRenderService;
        _publisher = publisher;
        _pendingJobService = pendingJobService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ApplicationResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        return applications
            .Where(a => !a.IsHistorical)
            .Select(a =>
            {
                var stage = a.Status is "Processing" or "Failed" or "Regenerating" or "Queued"
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
                    Notes = a.Notes.Select(n => new NoteResponse
                    {
                        Id = n.Id, Category = n.Category, Content = n.Content,
                        Stage = n.Stage, Pinned = n.Pinned,
                        Created_at = n.CreatedAt.ToString("o"),
                        Updated_at = n.UpdatedAt.ToString("o"),
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
                    Status = a.Status,
                    Has_content = !string.IsNullOrEmpty(a.Headline),
                };
            })
            .ToList();
    }

    public async Task<PagedResponse<ApplicationResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? stage,
        string sortBy, string sortDirection, CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetPagedApplicationsAsync(
            page, pageSize, search, stage, sortBy, sortDirection, cancellationToken);

        var items = result.Items.Select(a =>
        {
            var effectiveStage = a.Status is "Processing" or "Failed" or "Regenerating" or "Queued"
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
                Stage = effectiveStage,
                Personal_notes = a.PersonalNotes,
                Interviews = a.Interviews.Select(i => new InterviewResponse
                {
                    Id = i.Id, Date = i.Date, Type = i.Type, Notes = i.Notes, Outcome = i.Outcome,
                }).ToList(),
                Notes = a.Notes.Select(n => new NoteResponse
                {
                    Id = n.Id, Category = n.Category, Content = n.Content,
                    Stage = n.Stage, Pinned = n.Pinned,
                    Created_at = n.CreatedAt.ToString("o"),
                    Updated_at = n.UpdatedAt.ToString("o"),
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
                Status = a.Status,
                Has_content = !string.IsNullOrEmpty(a.Headline),
            };
        }).ToList();

        return new PagedResponse<ApplicationResponse>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize,
            StageCounts = result.StageCounts,
        };
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

    public async Task<InterviewListResponse?> AddInterviewAsync(string companyName, AddInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var interview = new InterviewRecord
        {
            Date = request.Date,
            Type = request.Type,
            Notes = request.Notes,
            Outcome = request.Outcome,
        };

        var application = await _repository.AddInterviewAsync(companyName, interview, cancellationToken);
        if (application == null) return null;

        return new InterviewListResponse
        {
            Ok = true,
            Interviews = application.Interviews.Select(i => new InterviewResponse
            {
                Id = i.Id, Date = i.Date, Type = i.Type, Notes = i.Notes, Outcome = i.Outcome,
            }).ToList(),
        };
    }

    public async Task<InterviewListResponse?> UpdateInterviewAsync(string companyName, int index, UpdateInterviewRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _repository.UpdateInterviewAsync(companyName, index, request.Date, request.Type, request.Notes, request.Outcome, cancellationToken);
        if (application == null) return null;

        return new InterviewListResponse
        {
            Ok = true,
            Interviews = application.Interviews.Select(i => new InterviewResponse
            {
                Id = i.Id, Date = i.Date, Type = i.Type, Notes = i.Notes, Outcome = i.Outcome,
            }).ToList(),
        };
    }

    public Task<bool> DeleteInterviewAsync(string companyName, int index, CancellationToken cancellationToken = default)
        => _repository.DeleteInterviewAsync(companyName, index, cancellationToken);

    public async Task<NoteListResponse?> AddNoteAsync(string companyName, AddNoteRequest request, CancellationToken cancellationToken = default)
    {
        var stage = request.Stage;
        if (string.IsNullOrEmpty(stage))
        {
            var app = await _repository.GetByCompanyNameAsync(companyName, cancellationToken);
            stage = app?.Stage is { Length: > 0 } s ? s : "Ready to Apply";
        }

        var note = new ApplicationNote
        {
            Category = request.Category,
            Content = request.Content,
            Pinned = request.Pinned,
            Stage = stage,
        };

        var application = await _repository.AddNoteAsync(companyName, note, cancellationToken);
        if (application == null) return null;

        return MapNoteListResponse(application);
    }

    public async Task<NoteListResponse?> UpdateNoteAsync(string companyName, int noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default)
    {
        var application = await _repository.UpdateNoteAsync(companyName, noteId, request.Category, request.Content, request.Pinned, cancellationToken);
        if (application == null) return null;

        return MapNoteListResponse(application);
    }

    public Task<bool> DeleteNoteAsync(string companyName, int noteId, CancellationToken cancellationToken = default)
        => _repository.DeleteNoteAsync(companyName, noteId, cancellationToken);

    private static NoteListResponse MapNoteListResponse(JobApplication application) => new()
    {
        Ok = true,
        Notes = application.Notes.Select(n => new NoteResponse
        {
            Id = n.Id, Category = n.Category, Content = n.Content,
            Stage = n.Stage, Pinned = n.Pinned,
            Created_at = n.CreatedAt.ToString("o"),
            Updated_at = n.UpdatedAt.ToString("o"),
        }).ToList(),
    };

    public async Task<ContentResponse?> GetContentAsync(string companyName, CancellationToken cancellationToken = default)
    {
        var app = await _repository.GetByCompanyNameAsync(companyName, cancellationToken);
        if (app == null) return null;

        return new ContentResponse
        {
            Headline = app.Headline ?? "",
            Summary = app.Summary ?? "",
            Skills = app.Skills,
            Roles = app.Roles,
            Recipient = app.Recipient ?? "",
            CoverLetterParagraphs = app.CoverLetterParagraphs,
            Strengths = app.Strengths,
            Gaps = app.Gaps,
        };
    }

    public async Task<bool> UpdateContentAsync(string companyName, UpdateContentRequest request, CancellationToken cancellationToken = default)
    {
        return await _repository.UpdateContentAsync(
            companyName,
            request.Headline,
            request.Summary,
            request.Skills,
            request.Roles,
            request.Recipient,
            request.CoverLetterParagraphs,
            request.Strengths,
            request.Gaps,
            cancellationToken);
    }

    public async Task<string?> RegenerateAsync(string companyName, UpdateContentRequest? contentUpdate, CancellationToken cancellationToken = default)
    {
        var app = await _repository.GetByCompanyNameAsync(companyName, cancellationToken);
        if (app == null) return null;

        if (contentUpdate != null)
        {
            await _repository.UpdateContentAsync(
                companyName,
                contentUpdate.Headline,
                contentUpdate.Summary,
                contentUpdate.Skills,
                contentUpdate.Roles,
                contentUpdate.Recipient,
                contentUpdate.CoverLetterParagraphs,
                contentUpdate.Strengths,
                contentUpdate.Gaps,
                cancellationToken);
        }

        await _repository.UpdateStatusAsync(app.Id!, "Regenerating", cancellationToken: cancellationToken);

        var jobEvent = new JobApplicationEvent
        {
            ApplicationId = app.Id,
            CompanyName = app.CompanyName,
            JobTitle = app.JobTitle,
            MatchScore = app.MatchScore,
            Recommendation = app.Recommendation,
            Status = "Regenerating",
            URL = app.JobUrl,
            EventType = "received",
            Timestamp = DateTime.UtcNow
        };

        await _publisher.PublishJobApplicationEventAsync(jobEvent);

        return app.Id;
    }

    public async Task<string?> ReQueueAsync(string companyName, string? prompt, CancellationToken cancellationToken = default)
    {
        var app = await _repository.GetByCompanyNameAsync(companyName, cancellationToken);
        if (app == null) return null;
        if (string.IsNullOrWhiteSpace(app.JobUrl)) return null;

        var job = await _pendingJobService.CreateAsync(app.JobUrl, prompt, cancellationToken);

        await _repository.UpdateStatusAsync(app.Id!, "Queued", cancellationToken: cancellationToken);

        return job.Id;
    }
}
