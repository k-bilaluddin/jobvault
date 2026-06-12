namespace JobVault.Application.Common;

public class FileIngestResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CommitUrl { get; set; } = string.Empty;
    public List<string> FilesUploaded { get; set; } = new();

    public static FileIngestResult Success(string companyName, string commitUrl, List<string> filesUploaded)
    {
        return new FileIngestResult
        {
            IsSuccess = true,
            CompanyName = companyName,
            CommitUrl = commitUrl,
            FilesUploaded = filesUploaded
        };
    }

    public static FileIngestResult Failure(string errorMessage)
    {
        return new FileIngestResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}