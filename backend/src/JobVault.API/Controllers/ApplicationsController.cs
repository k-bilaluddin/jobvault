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
    private readonly IPendingJobService _pendingJobService;

    public ApplicationsController(
        IApplicationQueryService queryService,
        IVaultFileService vaultFileService,
        ITokenService tokenService,
        IGitSyncService gitSyncService,
        IPendingJobService pendingJobService)
    {
        _queryService = queryService;
        _vaultFileService = vaultFileService;
        _tokenService = tokenService;
        _gitSyncService = gitSyncService;
        _pendingJobService = pendingJobService;
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

    [HttpPost("{id}/stage")]
    public async Task<IActionResult> UpdateStage(string id, [FromBody] UpdateStageRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdateStageAsync(id, request.Stage, cancellationToken);
        if (!success) return ErrorResponse("application.stage_update_failed", id);
        return Ok(new { ok = true, stage = request.Stage });
    }

    [HttpPost("{id}/personal-notes")]
    public async Task<IActionResult> UpdatePersonalNotes(string id, [FromBody] UpdateNotesRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdatePersonalNotesAsync(id, request.Notes, cancellationToken);
        if (!success) return ErrorResponse("application.notes_update_failed", id);
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/interviews")]
    public async Task<IActionResult> AddInterview(string id, [FromBody] AddInterviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.AddInterviewAsync(id, request, cancellationToken);
        if (result == null) return ErrorResponse("application.interview_add_failed", id);
        return Ok(result);
    }

    [HttpPut("{id}/interviews/{idx:int}")]
    public async Task<IActionResult> UpdateInterview(string id, int idx, [FromBody] UpdateInterviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.UpdateInterviewAsync(id, idx, request, cancellationToken);
        if (result == null) return ErrorResponse("application.interview_update_failed", id);
        return Ok(result);
    }

    [HttpDelete("{id}/interviews")]
    public async Task<IActionResult> DeleteInterview(string id, [FromQuery] int idx, CancellationToken cancellationToken)
    {
        var success = await _queryService.DeleteInterviewAsync(id, idx, cancellationToken);
        if (!success) return ErrorResponse("application.interview_del_failed", id);
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/notes")]
    public async Task<IActionResult> AddNote(string id, [FromBody] AddNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.AddNoteAsync(id, request, cancellationToken);
        if (result == null) return ErrorResponse("application.note_add_failed", id);
        return Ok(result);
    }

    [HttpPut("{id}/notes/{noteId:int}")]
    public async Task<IActionResult> UpdateNote(string id, int noteId, [FromBody] UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await _queryService.UpdateNoteAsync(id, noteId, request, cancellationToken);
        if (result == null) return ErrorResponse("application.note_update_failed", id);
        return Ok(result);
    }

    [HttpDelete("{id}/notes/{noteId:int}")]
    public async Task<IActionResult> DeleteNote(string id, int noteId, CancellationToken cancellationToken)
    {
        var success = await _queryService.DeleteNoteAsync(id, noteId, cancellationToken);
        if (!success) return ErrorResponse("application.note_del_failed", id);
        return Ok(new { ok = true });
    }

    [HttpGet("{id}/report")]
    public async Task<IActionResult> GetReport(string id, CancellationToken cancellationToken)
    {
        var html = await _queryService.GetReportHtmlAsync(id, cancellationToken);
        return Ok(new { html = html ?? "<p style='opacity:.5'>No compatibility report found.</p>" });
    }

    [HttpGet("{id}/notes")]
    public async Task<IActionResult> GetNotes(string id, CancellationToken cancellationToken)
    {
        var html = await _queryService.GetNotesHtmlAsync(id, cancellationToken);
        return Ok(new { html = html ?? "<p style='opacity:.5'>No tailoring notes found.</p>" });
    }

    [AllowAnonymous]
    [HttpGet("{id}/pdf/{type}")]
    public async Task<IActionResult> GetPdf(string id, string type, [FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (!_tokenService.ValidateToken(token)) return Unauthorized();

        var companyName = await _queryService.GetCompanyNameAsync(id, cancellationToken);
        if (companyName == null) return ErrorResponse("application.not_found", id);

        var bytes = await _vaultFileService.GetPdfBytesAsync(companyName, id, type, cancellationToken);
        if (bytes == null) return ErrorResponse("vault.file_not_found", type, companyName);

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

    [HttpGet("{id}/content")]
    public async Task<IActionResult> GetContent(string id, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetContentAsync(id, cancellationToken);
        if (result == null) return ErrorResponse("application.not_found", id);
        return Ok(result);
    }

    [HttpPatch("{id}/content")]
    public async Task<IActionResult> UpdateContent(string id, [FromBody] UpdateContentRequest request, CancellationToken cancellationToken)
    {
        var success = await _queryService.UpdateContentAsync(id, request, cancellationToken);
        if (!success) return ErrorResponse("application.not_found", id);
        return Ok(new { ok = true });
    }

    [HttpPost("{id}/regenerate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Regenerate(string id, [FromBody] UpdateContentRequest? request, CancellationToken cancellationToken)
    {
        var applicationId = await _queryService.RegenerateAsync(id, request, cancellationToken);
        if (applicationId == null) return ErrorResponse("application.not_found", id);
        return Accepted(new { ok = true, applicationId });
    }

    [HttpPost("{id}/re-queue")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReQueue(string id, [FromBody] ReQueueRequest? request, CancellationToken cancellationToken)
    {
        var jobId = await _queryService.ReQueueAsync(id, request?.Prompt, cancellationToken);
        if (jobId == null) return ErrorResponse("application.not_found", id);
        return Accepted(new { ok = true, jobId });
    }

    [HttpPost("sync-vault")]
    public async Task<IActionResult> SyncVault(CancellationToken cancellationToken)
    {
        var result = await _gitSyncService.SyncAsync(cancellationToken);
        return Ok(result);
    }
}
