namespace JobVault.Contracts.Requests;

public class UpdateSettingsRequest
{
    public GitHubSettingsRequest GitHub { get; set; } = new();
    public TelegramSettingsRequest Telegram { get; set; } = new();
}

public class GitHubSettingsRequest
{
    public string Owner { get; set; } = "";
    public string Repository { get; set; } = "";
    public string Branch { get; set; } = "";
    public string CvFileName { get; set; } = "";
    public string CoverLetterFileName { get; set; } = "";
}

public class TelegramSettingsRequest
{
    public string ChatId { get; set; } = "";
}
