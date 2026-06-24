namespace JobVault.Domain.Entities;

public class AppSettings
{
    public string? Id { get; set; }

    // GitHub
    public string GitHubOwner { get; set; } = "k-bilaluddin";
    public string GitHubRepository { get; set; } = "job-applications-vault";
    public string GitHubBranch { get; set; } = "master";
    public string GitHubCvFileName { get; set; } = "KhawajaBilal_Uddin_CV";
    public string GitHubCoverLetterFileName { get; set; } = "KhawajaBilal_Uddin_CoverLetter";

    // Telegram
    public string TelegramChatId { get; set; } = "";
}
