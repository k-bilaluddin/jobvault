using JobVault.API.Filters;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[Route("api/ingest/queue")]
public class JobQueueController : ApiControllerBase
{
    private readonly IPendingJobService _service;

    public JobQueueController(IPendingJobService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    [ApiKey]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var jobs = await _service.GetPendingAsync(ct);
        var response = jobs.Select(j => new { jobId = j.Id, url = j.Url, prompt = j.Prompt });
        return Ok(response);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var jobs = await _service.GetAllAsync(status, ct);
        return Ok(jobs.Select(ToResponse));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePendingJobRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return ErrorResponse("queue.url_required");

        var job = await _service.CreateAsync(request.Url.Trim(), request.Prompt?.Trim(), ct);
        return Created($"/api/ingest/queue/{job.Id}", ToResponse(job));
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePendingJobRequest request, CancellationToken ct)
    {
        var ok = await _service.UpdateAsync(id, request.Url, request.Status, ct);
        if (!ok) return ErrorResponse("queue.not_found", id);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var ok = await _service.DeleteAsync(id, ct);
        if (!ok) return ErrorResponse("queue.not_found", id);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("cleanup/{status}")]
    public async Task<IActionResult> Cleanup(string status, CancellationToken ct)
    {
        var count = await _service.CleanupByStatusAsync(status, ct);
        return Ok(new { deleted = count });
    }

    private static PendingJobResponse ToResponse(Domain.Entities.PendingJob j) => new()
    {
        JobId = j.Id,
        Url = j.Url,
        Status = j.Status,
        Prompt = j.Prompt,
        CreatedAt = j.CreatedAt,
        UpdatedAt = j.UpdatedAt,
    };
}
