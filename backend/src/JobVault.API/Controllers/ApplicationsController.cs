using JobVault.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Authorize]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IJobApplicationRepository _repository;

    public ApplicationsController(IJobApplicationRepository repository)
    {
        _repository = repository;
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
}
