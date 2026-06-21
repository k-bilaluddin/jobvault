using JobVault.Contracts.Responses;
using JobVault.Domain.Entities;
using JobVault.Domain.ValueObjects;

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
    Task<JobApplication?> AddInterviewAsync(string companyName, InterviewRecord interview, CancellationToken cancellationToken = default);
    Task<bool> DeleteInterviewAsync(string companyName, int index, CancellationToken cancellationToken = default);
}
