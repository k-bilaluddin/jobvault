using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;

namespace JobVault.Application.Interfaces;

public interface IApplicationQueryService
{
    Task<IReadOnlyList<ApplicationResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<string?> GetReportHtmlAsync(string companyName, CancellationToken cancellationToken = default);
    Task<string?> GetNotesHtmlAsync(string companyName, CancellationToken cancellationToken = default);
    Task<SkillsGapResponse> GetSkillsGapAsync(CancellationToken cancellationToken = default);
    Task<HistoricalResponse> GetHistoricalAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateStageAsync(string companyName, string stage, CancellationToken cancellationToken = default);
    Task<bool> UpdatePersonalNotesAsync(string companyName, string notes, CancellationToken cancellationToken = default);
    Task<InterviewListResponse?> AddInterviewAsync(string companyName, AddInterviewRequest request, CancellationToken cancellationToken = default);
    Task<InterviewListResponse?> UpdateInterviewAsync(string companyName, int index, UpdateInterviewRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteInterviewAsync(string companyName, int index, CancellationToken cancellationToken = default);
    Task<NoteListResponse?> AddNoteAsync(string companyName, AddNoteRequest request, CancellationToken cancellationToken = default);
    Task<NoteListResponse?> UpdateNoteAsync(string companyName, int noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteNoteAsync(string companyName, int noteId, CancellationToken cancellationToken = default);
}
