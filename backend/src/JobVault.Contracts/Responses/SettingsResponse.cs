namespace JobVault.Contracts.Responses;

public class SettingsResponse
{
    public GitHubSettings GitHub { get; init; } = new();
    public TelegramSettings Telegram { get; init; } = new();
}

public class GitHubSettings
{
    public string Owner { get; init; } = "";
    public string Repository { get; init; } = "";
    public string Branch { get; init; } = "";
    public string CvFileName { get; init; } = "";
    public string CoverLetterFileName { get; init; } = "";
}

public class TelegramSettings
{
    public string ChatId { get; init; } = "";
}
