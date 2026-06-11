namespace JobVault.Contracts.Responses;

public class IngestResponse
{
    public string CompanyName { get; set; } = string.Empty;
    public string CommitUrl { get; set; } = string.Empty;
    public List<string> FilesUploaded { get; set; } = new();
}