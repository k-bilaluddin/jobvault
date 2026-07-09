namespace JobVault.Application.Interfaces;

public interface IVaultFileService
{
    (bool HasCvPdf, bool HasLetterPdf, bool HasReport, bool HasNotes) CheckFiles(string companyName, string id);
    string? ReadMarkdown(string companyName, string id, string[] fileNames);
    Task<byte[]?> GetPdfBytesAsync(string companyName, string id, string type, CancellationToken cancellationToken = default);
    void EvictCache(string companyName, string id);
}
