namespace JobVault.Application.Interfaces;

public interface IVaultFileService
{
    (bool HasCvPdf, bool HasLetterPdf, bool HasReport, bool HasNotes) CheckFiles(string companyName);
    string? ReadMarkdown(string companyName, string[] fileNames);
    Task<byte[]?> GetPdfBytesAsync(string companyName, string type, CancellationToken cancellationToken = default);
    void EvictCache(string companyName);
}
