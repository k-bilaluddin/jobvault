using JobVault.Application.Interfaces;
using JobVault.Contracts.Requests;
using JobVault.Contracts.Responses;
using JobVault.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[Authorize]
[Route("api/settings")]
public class SettingsController : ApiControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        return Ok(MapToResponse(settings));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);

        settings.GitHubOwner = request.GitHub.Owner;
        settings.GitHubRepository = request.GitHub.Repository;
        settings.GitHubBranch = request.GitHub.Branch;
        settings.GitHubCvFileName = request.GitHub.CvFileName;
        settings.GitHubCoverLetterFileName = request.GitHub.CoverLetterFileName;
        settings.TelegramChatId = request.Telegram.ChatId;

        var saved = await _settingsService.SaveAsync(settings, cancellationToken);
        return Ok(MapToResponse(saved));
    }

    private static SettingsResponse MapToResponse(AppSettings s) => new()
    {
        GitHub = new GitHubSettings
        {
            Owner = s.GitHubOwner,
            Repository = s.GitHubRepository,
            Branch = s.GitHubBranch,
            CvFileName = s.GitHubCvFileName,
            CoverLetterFileName = s.GitHubCoverLetterFileName,
        },
        Telegram = new TelegramSettings
        {
            ChatId = s.TelegramChatId,
        },
    };
}
