namespace JobVault.Contracts.Responses;

public class FileMetadataResponse
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}