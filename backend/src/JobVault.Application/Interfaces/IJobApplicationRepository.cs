using JobVault.Application.Common;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;

namespace JobVault.Application.Interfaces;

public record ApplicationPageResult(
    IReadOnlyList<JobApplication> Items,
    int TotalCount,
    Dictionary<string, int> StageCounts);

/// <summary>
/// Repository interface for job application persistence operations.
/// </summary>
public interface IJobApplicationRepository
{
    /// <summary>
    /// Resolves an incoming ingestion against existing applications and inserts, updates, or no-ops
    /// per the identity resolution cascade (normalized URL match -> company+title fallback match ->
    /// status-guard -> insert). See issue #104. Returns the MongoDB ObjectId in UpsertResult.Id.
    /// </summary>
    Task<UpsertResult> UpsertApplicationAsync(JobApplication application);

    /// <summary>
    /// Updates an already-known application unconditionally, bypassing the resolution cascade and
    /// status-guard entirely. Used when the caller already knows the exact target (e.g. a re-queue
    /// completing via its linked PendingJob.SourceApplicationId) rather than inferring a match.
    /// </summary>
    Task<UpsertResult> UpdateApplicationByIdAsync(string applicationId, JobApplication application, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a job application by its MongoDB ObjectId string.
    /// </summary>
    Task<JobApplication?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates status and optional fields on an existing document. Clears blob fields on terminal states.
    /// </summary>
    Task<bool> UpdateStatusAsync(
        string id,
        string status,
        string? commitUrl = null,
        string? errorDetails = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobApplication>> GetAllApplicationsAsync(CancellationToken cancellationToken = default);

    Task<ApplicationPageResult> GetPagedApplicationsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? stage = null,
        string sortBy = "synced_at",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStageAsync(string id, string stage, CancellationToken cancellationToken = default);

    Task<bool> UpdatePersonalNotesAsync(string id, string notes, CancellationToken cancellationToken = default);

    Task<JobApplication?> AddInterviewAsync(string id, InterviewRecord interview, CancellationToken cancellationToken = default);

    Task<JobApplication?> UpdateInterviewAsync(string id, int index, string? date, string? type, string? notes, string? outcome, CancellationToken cancellationToken = default);
    Task<bool> DeleteInterviewAsync(string id, int index, CancellationToken cancellationToken = default);

    Task<JobApplication?> AddNoteAsync(string id, ApplicationNote note, CancellationToken cancellationToken = default);
    Task<JobApplication?> UpdateNoteAsync(string id, int noteId, string? category, string? content, bool? pinned, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(string id, int noteId, CancellationToken cancellationToken = default);

    Task<bool> UpdateContentAsync(
        string id,
        string? headline,
        string? summary,
        List<Domain.ValueObjects.SkillRow>? skills,
        List<Domain.ValueObjects.RolePayload>? roles,
        string? recipient,
        List<string>? coverLetterParagraphs,
        List<string>? strengths,
        List<string>? gaps,
        CancellationToken cancellationToken = default);
}