using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;

namespace JobVault.Application.Interfaces;

public interface IApplicationQueryService
{
    Task<IReadOnlyList<ApplicationResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<ApplicationResponse>> GetPagedAsync(int page, int pageSize, string? search, string? stage, string sortBy, string sortDirection, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellationToken = default);
    Task<string?> GetCompanyNameAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetReportHtmlAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetNotesHtmlAsync(string id, CancellationToken cancellationToken = default);
    Task<SkillsGapResponse> GetSkillsGapAsync(CancellationToken cancellationToken = default);
    Task<HistoricalResponse> GetHistoricalAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStageAsync(string id, string stage, CancellationToken cancellationToken = default);
    Task<bool> UpdatePersonalNotesAsync(string id, string notes, CancellationToken cancellationToken = default);
    Task<bool> UpdateDisplayNameAsync(string id, string? displayName, CancellationToken cancellationToken = default);
    Task<InterviewListResponse?> AddInterviewAsync(string id, AddInterviewRequest request, CancellationToken cancellationToken = default);
    Task<InterviewListResponse?> UpdateInterviewAsync(string id, int index, UpdateInterviewRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteInterviewAsync(string id, int index, CancellationToken cancellationToken = default);
    Task<NoteListResponse?> AddNoteAsync(string id, AddNoteRequest request, CancellationToken cancellationToken = default);
    Task<NoteListResponse?> UpdateNoteAsync(string id, int noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(string id, int noteId, CancellationToken cancellationToken = default);
    Task<ContentResponse?> GetContentAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> UpdateContentAsync(string id, UpdateContentRequest request, CancellationToken cancellationToken = default);
    Task<string?> RegenerateAsync(string id, UpdateContentRequest? contentUpdate, CancellationToken cancellationToken = default);
    Task<string?> ReQueueAsync(string id, string? prompt, CancellationToken cancellationToken = default);
}
