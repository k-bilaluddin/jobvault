namespace JobVault.Domain.Entities;

public class AppSettings
{
    public string? Id { get; set; }

    // GitHub
    public string GitHubOwner { get; set; } = "";
    public string GitHubRepository { get; set; } = "";
    public string GitHubBranch { get; set; } = "master";
    public string GitHubCvFileName { get; set; } = "";
    public string GitHubCoverLetterFileName { get; set; } = "";

    // Telegram
    public string TelegramChatId { get; set; } = "";
}
