using JobVault.Application.Interfaces;
using JobVault.Contracts.Requests;
using JobVault.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
[Authorize]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationQueryService _queryService;
    private readonly IVaultFileService _vaultFileService;
    private readonly ITokenService _tokenService;
    private readonly IGitSyncService _gitSyncService;

    public ApplicationsController(
        IApplicationQueryService queryService,
        IVaultFileService vaultFileService,
        ITokenService tokenService,
        IGitSyncService gitSyncService)
    {
        _queryService = queryService;
        _vaultFileService = vaultFileService;
        _tokenService = tokenService;
        _gitSyncService = gitSyncService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _queryService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("{name}/stage")]
    public async Task<IActionResult> UpdateStage(string name, [FromBody] UpdateStageRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdateStageAsync(name, request.Stage, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { ok = true, stage = request.Stage });
    }

    [HttpPost("{name}/personal-notes")]
    public async Task<IActionResult> UpdatePersonalNotes(string name, [FromBody] UpdateNotesRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdatePersonalNotesAsync(name, request.Notes, cancellationToken);
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

        var application = await _queryService.AddInterviewAsync(name, interview, cancellationToken);
        if (application == null) return NotFound();

        return Ok(new
        {
            ok = true,
            interviews = application.Interviews.Select(i => new
            {
                id = i.Id, date = i.Date, type = i.Type, notes = i.Notes, outcome = i.Outcome,
            }),
        });
    }

    [HttpDelete("{name}/interviews")]
    public async Task<IActionResult> DeleteInterview(string name, [FromQuery] int idx, CancellationToken cancellationToken)
    {
        var success = await _queryService.DeleteInterviewAsync(name, idx, cancellationToken);
        if (!success) return NotFound();
        return Ok(new { ok = true });
    }

    [HttpGet("{name}/report")]
    public async Task<IActionResult> GetReport(string name, CancellationToken cancellationToken)
    {
        var html = await _queryService.GetReportHtmlAsync(name, cancellationToken);
        return Ok(new { html = html ?? "<p style='opacity:.5'>No compatibility report found.</p>" });
    }

    [HttpGet("{name}/notes")]
    public async Task<IActionResult> GetNotes(string name, CancellationToken cancellationToken)
    {
        var html = await _queryService.GetNotesHtmlAsync(name, cancellationToken);
        return Ok(new { html = html ?? "<p style='opacity:.5'>No tailoring notes found.</p>" });
    }

    [AllowAnonymous]
    [HttpGet("{name}/pdf/{type}")]
    public IActionResult GetPdf(string name, string type, [FromQuery] string? token)
    {
        if (!_tokenService.ValidateToken(token)) return Unauthorized();

        var path = _vaultFileService.GetPdfPath(name, type);
        if (path == null) return NotFound();

        return PhysicalFile(path, "application/pdf");
    }

    [HttpGet("skills-gap")]
    public async Task<IActionResult> GetSkillsGap(CancellationToken cancellationToken)
    {
        var result = await _queryService.GetSkillsGapAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("historical")]
    public async Task<IActionResult> GetHistorical(CancellationToken cancellationToken)
    {
        var result = await _queryService.GetHistoricalAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("sync-vault")]
    public IActionResult SyncVault()
    {
        var result = _gitSyncService.Sync();
        return Ok(result);
    }
}
