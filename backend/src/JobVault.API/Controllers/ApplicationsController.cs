using JobVault.Application.Interfaces;
using JobVault.Contracts.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[Authorize]
[Route("api/applications")]
public class ApplicationsController : ApiControllerBase
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
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? stage = null,
        [FromQuery] string sortBy = "synced_at",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        if (page == null)
        {
            var all = await _queryService.GetAllAsync(cancellationToken);
            return Ok(all);
        }

        var p = page.Value < 1 ? 1 : page.Value;
        if (pageSize is < 1 or > 50) pageSize = 10;

        var result = await _queryService.GetPagedAsync(p, pageSize, search, stage, sortBy, sortDirection, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{name}/stage")]
    public async Task<IActionResult> UpdateStage(string name, [FromBody] UpdateStageRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdateStageAsync(name, request.Stage, cancellationToken);
        if (!success) return ErrorResponse("application.stage_update_failed", name);
        return Ok(new { ok = true, stage = request.Stage });
    }

    [HttpPost("{name}/personal-notes")]
    public async Task<IActionResult> UpdatePersonalNotes(string name, [FromBody] UpdateNotesRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdatePersonalNotesAsync(name, request.Notes, cancellationToken);
        if (!success) return ErrorResponse("application.notes_update_failed", name);
        return Ok(new { ok = true });
    }

    [HttpPost("{name}/interviews")]
    public async Task<IActionResult> AddInterview(string name, [FromBody] AddInterviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.AddInterviewAsync(name, request, cancellationToken);
        if (result == null) return ErrorResponse("application.interview_add_failed", name);
        return Ok(result);
    }

    [HttpPut("{name}/interviews/{idx:int}")]
    public async Task<IActionResult> UpdateInterview(string name, int idx, [FromBody] UpdateInterviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.UpdateInterviewAsync(name, idx, request, cancellationToken);
        if (result == null) return ErrorResponse("application.interview_update_failed", name);
        return Ok(result);
    }

    [HttpDelete("{name}/interviews")]
    public async Task<IActionResult> DeleteInterview(string name, [FromQuery] int idx, CancellationToken cancellationToken)
    {
        var success = await _queryService.DeleteInterviewAsync(name, idx, cancellationToken);
        if (!success) return ErrorResponse("application.interview_del_failed", name);
        return Ok(new { ok = true });
    }

    [HttpPost("{name}/notes")]
    public async Task<IActionResult> AddNote(string name, [FromBody] AddNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.AddNoteAsync(name, request, cancellationToken);
        if (result == null) return ErrorResponse("application.note_add_failed", name);
        return Ok(result);
    }

    [HttpPut("{name}/notes/{noteId:int}")]
    public async Task<IActionResult> UpdateNote(string name, int noteId, [FromBody] UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.UpdateNoteAsync(name, noteId, request, cancellationToken);
        if (result == null) return ErrorResponse("application.note_update_failed", name);
        return Ok(result);
    }

    [HttpDelete("{name}/notes/{noteId:int}")]
    public async Task<IActionResult> DeleteNote(string name, int noteId, CancellationToken cancellationToken)
    {
        var success = await _queryService.DeleteNoteAsync(name, noteId, cancellationToken);
        if (!success) return ErrorResponse("application.note_del_failed", name);
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
    public async Task<IActionResult> GetPdf(string name, string type, [FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (!_tokenService.ValidateToken(token)) return Unauthorized();

        var bytes = await _vaultFileService.GetPdfBytesAsync(name, type, cancellationToken);
        if (bytes == null) return ErrorResponse("vault.file_not_found", type, name);

        return File(bytes, "application/pdf");
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

    [HttpGet("{name}/content")]
    public async Task<IActionResult> GetContent(string name, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetContentAsync(name, cancellationToken);
        if (result == null) return ErrorResponse("application.not_found", name);
        return Ok(result);
    }

    [HttpPatch("{name}/content")]
    public async Task<IActionResult> UpdateContent(string name, [FromBody] UpdateContentRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdateContentAsync(name, request, cancellationToken);
        if (!success) return ErrorResponse("application.not_found", name);
        return Ok(new { ok = true });
    }

    [HttpPost("{name}/regenerate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Regenerate(string name, [FromBody] UpdateContentRequest? request, CancellationToken cancellationToken)
    {
        var applicationId = await _queryService.RegenerateAsync(name, request, cancellationToken);
        if (applicationId == null) return ErrorResponse("application.not_found", name);
        return Accepted(new { ok = true, applicationId });
    }

    [HttpPost("sync-vault")]
    public async Task<IActionResult> SyncVault(CancellationToken cancellationToken)
    {
        var result = await _gitSyncService.SyncAsync(cancellationToken);
        return Ok(result);
    }
}
