namespace JobVault.Application.Interfaces;

public interface IVaultFileService
{
    (bool HasCvPdf, bool HasLetterPdf, bool HasReport, bool HasNotes) CheckFiles(string companyName);
    string? ReadMarkdown(string companyName, string[] fileNames);
    string? GetPdfPath(string companyName, string type);
}
