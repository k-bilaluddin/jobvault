namespace JobVault.Application.Models;

public class IngestedFile
{
    public string FileName { get; set; } = string.Empty;
    public Stream Content { get; set; } = Stream.Null;
    public long Length { get; set; }
}