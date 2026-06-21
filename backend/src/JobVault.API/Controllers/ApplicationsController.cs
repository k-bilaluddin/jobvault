using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Requests;
using JobVault.Domain.ValueObjects;
using Markdig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace JobVault.API.Controllers;

[ApiController]
[Authorize]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IJobApplicationRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly string? _vaultRootDir;
    private readonly MarkdownPipeline _markdownPipeline;

    private static readonly Regex CvPattern = new(
        @"(?<![a-zA-Z])(cv|resume|lebenslauf)(?![a-zA-Z])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LetterPattern = new(
        @"cover.?letter|coverletter|anschreiben", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ApplicationsController(IJobApplicationRepository repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
        _vaultRootDir = configuration["Vault:RootDir"];
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        var result = applications
            .Where(a => !a.IsHistorical)
            .Select(a =>
            {
                var stage = a.Status is "Processing" or "Failed"
                    ? a.Status
                    : string.IsNullOrEmpty(a.Stage) ? "Ready to Apply" : a.Stage;

                return new
                {
                    name = a.CompanyName,
                    synced_at = a.UpdatedAt.ToString("o"),
                    has_report = !string.IsNullOrEmpty(a.CompatibilityReportMarkdown),
                    has_notes = !string.IsNullOrEmpty(a.TailoringNotesMarkdown),
                    has_cv_pdf = !string.IsNullOrEmpty(a.CommitUrl),
                    has_letter_pdf = !string.IsNullOrEmpty(a.CommitUrl),
                    match_pct = a.MatchScore > 0 ? (int?)a.MatchScore : null,
                    recommend = a.Recommendation,
                    job_url = a.JobUrl,
                    role = a.JobTitle,
                    applied = a.Applied,
                    applied_date = a.AppliedDate?.ToString("yyyy-MM-dd") ?? "",
                    stage,
                    personal_notes = a.PersonalNotes,
                    interviews = a.Interviews.Select(i => new
                    {
                        id = i.Id,
                        date = i.Date,
                        type = i.Type,
                        notes = i.Notes,
                        outcome = i.Outcome,
                    }),
                    salary = new
                    {
                        advertised = a.Salary.Advertised,
                        target = a.Salary.Target,
                        discussed = a.Salary.Discussed,
                        offered = a.Salary.Offered,
                    },
                    recruiter = new
                    {
                        name = a.Recruiter.Name,
                        email = a.Recruiter.Email,
                        linkedin = a.Recruiter.LinkedIn,
                    },
                    follow_up_date = a.FollowUpDate?.ToString("yyyy-MM-dd") ?? "",
                    source = a.Source,
                };
            });

        return Ok(result);
    }

    [HttpPost("{name}/stage")]
    public async Task<IActionResult> UpdateStage(string name, [FromBody] UpdateStageRequest request, CancellationToken cancellationToken)
    {
        var success = await _repository.UpdateStageAsync(name, request.Stage, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { ok = true, stage = request.Stage });
    }

    [HttpPost("{name}/personal-notes")]
    public async Task<IActionResult> UpdatePersonalNotes(string name, [FromBody] UpdateNotesRequest request, CancellationToken cancellationToken)
    {
        var success = await _repository.UpdatePersonalNotesAsync(name, request.Notes, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { ok = true });
    }

    [HttpPost("{name}/interviews")]
    public async Task<IActionResult> AddInterview(string name, [FromBody] AddInterviewRequest request, CancellationToken cancellationToken)
    {
        var interview = new InterviewRecord
        {
            Date = request.Date,
            Type = request.Type,
            Notes = request.Notes,
            Outcome = request.Outcome,
        };

        var application = await _repository.AddInterviewAsync(name, interview, cancellationToken);
        if (application == null) return NotFound();

        return Ok(new
        {
            ok = true,
            interviews = application.Interviews.Select(i => new
            {
                id = i.Id,
                date = i.Date,
                type = i.Type,
                notes = i.Notes,
                outcome = i.Outcome,
            }),
        });
    }

    [HttpDelete("{name}/interviews")]
    public async Task<IActionResult> DeleteInterview(string name, [FromQuery] int idx, CancellationToken cancellationToken)
    {
        var success = await _repository.DeleteInterviewAsync(name, idx, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { ok = true });
    }

    [HttpGet("{name}/report")]
    public async Task<IActionResult> GetReport(string name, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByCompanyNameAsync(name, cancellationToken);
        if (app == null) return NotFound();

        var markdown = app.CompatibilityReportMarkdown;

        if (string.IsNullOrEmpty(markdown))
            markdown = ReadVaultMarkdown(name, ["compatibility-report", "compatibility_report", "report"]);

        if (string.IsNullOrEmpty(markdown))
            return Ok(new { html = "<p style='opacity:.5'>No compatibility report found.</p>" });

        var html = Markdown.ToHtml(markdown, _markdownPipeline);
        return Ok(new { html });
    }

    [HttpGet("{name}/notes")]
    public async Task<IActionResult> GetNotes(string name, CancellationToken cancellationToken)
    {
        var app = await _repository.GetByCompanyNameAsync(name, cancellationToken);
        if (app == null) return NotFound();

        var markdown = app.TailoringNotesMarkdown;

        if (string.IsNullOrEmpty(markdown))
            markdown = ReadVaultMarkdown(name, ["tailoring-notes", "tailoring_notes", "notes"]);

        if (string.IsNullOrEmpty(markdown))
            return Ok(new { html = "<p style='opacity:.5'>No tailoring notes found.</p>" });

        var html = Markdown.ToHtml(markdown, _markdownPipeline);
        return Ok(new { html });
    }

    [AllowAnonymous]
    [HttpGet("{name}/pdf/{type}")]
    public IActionResult GetPdf(string name, string type, [FromQuery] string? token)
    {
        if (!ValidateQueryToken(token)) return Unauthorized();

        if (string.IsNullOrEmpty(_vaultRootDir))
            return StatusCode(503, new { error = "Vault root directory not configured" });

        var folder = Path.Combine(_vaultRootDir, name);
        if (!Directory.Exists(folder)) return NotFound();

        var pattern = type == "cv" ? CvPattern : LetterPattern;

        var pdf = Directory.EnumerateFiles(folder)
            .Where(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(f => pattern.IsMatch(Path.GetFileName(f)));

        if (pdf == null) return NotFound();

        return PhysicalFile(pdf, "application/pdf");
    }

    [HttpGet("skills-gap")]
    public async Task<IActionResult> GetSkillsGap(CancellationToken cancellationToken)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        var skillCounts = new Dictionary<string, (string Skill, int Count, List<string> Companies)>(StringComparer.OrdinalIgnoreCase);

        foreach (var app in applications.Where(a => !a.IsHistorical && a.Gaps.Count > 0))
        {
            foreach (var gap in app.Gaps)
            {
                var key = gap.ToLowerInvariant();
                if (!skillCounts.TryGetValue(key, out var entry))
                {
                    entry = (gap, 0, []);
                    skillCounts[key] = entry;
                }

                if (!entry.Companies.Contains(app.CompanyName))
                {
                    entry.Count++;
                    entry.Companies.Add(app.CompanyName);
                    skillCounts[key] = entry;
                }
            }
        }

        var ranked = skillCounts.Values
            .OrderByDescending(x => x.Count)
            .Select(x => new { skill = x.Skill, count = x.Count, companies = x.Companies });

        var reportsScanned = applications.Count(a => !a.IsHistorical && !string.IsNullOrEmpty(a.CompatibilityReportMarkdown));

        return Ok(new { gaps = ranked, reports_scanned = reportsScanned });
    }

    [HttpGet("historical")]
    public async Task<IActionResult> GetHistorical(CancellationToken cancellationToken)
    {
        var applications = await _repository.GetAllApplicationsAsync(cancellationToken);

        var historical = applications
            .Where(a => a.IsHistorical)
            .Select(a => new
            {
                name = a.CompanyName,
                applied_date = a.AppliedDate?.ToString("yyyy-MM-dd") ?? "",
                stage = string.IsNullOrEmpty(a.Stage) ? "Applied" : a.Stage,
                source = a.Source,
            })
            .ToList();

        var current = applications
            .Where(a => !a.IsHistorical && a.Applied)
            .Select(a => new
            {
                name = a.CompanyName,
                applied_date = a.AppliedDate?.ToString("yyyy-MM-dd") ?? "",
                stage = string.IsNullOrEmpty(a.Stage) ? "Applied" : a.Stage,
                source = a.Source,
                current = true,
            })
            .ToList();

        return Ok(new
        {
            available = true,
            historical,
            current,
            scanned_at = DateTime.UtcNow.ToString("o"),
        });
    }

    [HttpPost("sync-vault")]
    public IActionResult SyncVault()
    {
        if (string.IsNullOrEmpty(_vaultRootDir))
            return StatusCode(503, new { ok = false, message = "Vault root directory not configured" });

        if (!Directory.Exists(_vaultRootDir))
            return Ok(new { ok = false, message = "Vault directory not found" });

        try
        {
            var psi = new ProcessStartInfo("git", "pull origin master")
            {
                WorkingDirectory = _vaultRootDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
                return Ok(new { ok = false, message = "Failed to start git process" });

            process.WaitForExit(30000);

            var stdout = process.StandardOutput.ReadToEnd().Trim();
            var stderr = process.StandardError.ReadToEnd().Trim();

            return process.ExitCode == 0
                ? Ok(new { ok = true, message = string.IsNullOrEmpty(stdout) ? "Already up to date." : stdout })
                : Ok(new { ok = false, message = stderr });
        }
        catch (Exception ex)
        {
            return Ok(new { ok = false, message = ex.Message });
        }
    }

    private string? ReadVaultMarkdown(string companyName, string[] fileNames)
    {
        if (string.IsNullOrEmpty(_vaultRootDir)) return null;

        var folder = Path.Combine(_vaultRootDir, companyName);
        if (!Directory.Exists(folder)) return null;

        var extensions = new[] { ".md", ".txt" };

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var stem = Path.GetFileNameWithoutExtension(file).ToLowerInvariant().Replace("-", "_");
            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (extensions.Contains(ext) && fileNames.Any(n => n.Replace("-", "_") == stem))
                return System.IO.File.ReadAllText(file);
        }

        return null;
    }

    private bool ValidateQueryToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        var secret = _configuration["Auth:JwtSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return false;

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
