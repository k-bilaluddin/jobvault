using System.Text.RegularExpressions;
using JobVault.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JobVault.Infrastructure.Vault;

public class VaultFileService : IVaultFileService
{
    private readonly string? _rootDir;

    private static readonly Regex CvPattern = new(
        @"(?<![a-zA-Z])(cv|resume|lebenslauf)(?![a-zA-Z])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LetterPattern = new(
        @"cover.?letter|coverletter|anschreiben", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> ReportNames = ["compatibility_report", "report"];
    private static readonly HashSet<string> NotesNames = ["tailoring_notes", "notes"];

    public VaultFileService(IConfiguration configuration)
    {
        _rootDir = configuration["Vault:RootDir"];
    }

    public (bool HasCvPdf, bool HasLetterPdf, bool HasReport, bool HasNotes) CheckFiles(string companyName)
    {
        if (string.IsNullOrEmpty(_rootDir)) return (false, false, false, false);

        var folder = Path.Combine(_rootDir, companyName);
        if (!Directory.Exists(folder)) return (false, false, false, false);

        bool hasCv = false, hasLetter = false, hasReport = false, hasNotes = false;

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            var name = Path.GetFileName(file);
            var stem = Path.GetFileNameWithoutExtension(file).ToLowerInvariant().Replace("-", "_");

            if (ext == ".pdf")
            {
                if (CvPattern.IsMatch(name)) hasCv = true;
                if (LetterPattern.IsMatch(name)) hasLetter = true;
            }
            else if (ext is ".md" or ".txt")
            {
                if (ReportNames.Contains(stem)) hasReport = true;
                if (NotesNames.Contains(stem)) hasNotes = true;
            }
        }

        return (hasCv, hasLetter, hasReport, hasNotes);
    }

    public string? ReadMarkdown(string companyName, string[] fileNames)
    {
        if (string.IsNullOrEmpty(_rootDir)) return null;

        var folder = Path.Combine(_rootDir, companyName);
        if (!Directory.Exists(folder)) return null;

        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var stem = Path.GetFileNameWithoutExtension(file).ToLowerInvariant().Replace("-", "_");
            var ext = Path.GetExtension(file).ToLowerInvariant();

            if (ext is ".md" or ".txt" && fileNames.Any(n => n.Replace("-", "_") == stem))
                return File.ReadAllText(file);
        }

        return null;
    }

    public string? GetPdfPath(string companyName, string type)
    {
        if (string.IsNullOrEmpty(_rootDir)) return null;

        var folder = Path.Combine(_rootDir, companyName);
        if (!Directory.Exists(folder)) return null;

        var pattern = type == "cv" ? CvPattern : LetterPattern;

        return Directory.EnumerateFiles(folder)
            .Where(f => Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(f => pattern.IsMatch(Path.GetFileName(f)));
    }
}
