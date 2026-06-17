using System.Diagnostics;
using JobVault.Application.Common;
using JobVault.Application.Interfaces;
using JobVault.Application.Models;
using JobVault.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Processing;

public class ApplicationProcessorService : IApplicationProcessorService
{
    private readonly IJobApplicationRepository _repository;
    private readonly IDocumentGenerationClient _generationClient;
    private readonly IFileIngestService _fileIngestService;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationProcessorService> _logger;

    public ApplicationProcessorService(
        IJobApplicationRepository repository,
        IDocumentGenerationClient generationClient,
        IFileIngestService fileIngestService,
        IRabbitMqPublisher publisher,
        IConfiguration configuration,
        ILogger<ApplicationProcessorService> logger)
    {
        _repository = repository;
        _generationClient = generationClient;
        _fileIngestService = fileIngestService;
        _publisher = publisher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting processing for applicationId={Id}", applicationId);

        var application = await _repository.GetByIdAsync(applicationId, cancellationToken);
        if (application == null)
        {
            _logger.LogError("Application not found: {Id}", applicationId);
            return;
        }

        if (string.IsNullOrWhiteSpace(application.Headline) ||
            string.IsNullOrWhiteSpace(application.CompatibilityReportMarkdown) ||
            string.IsNullOrWhiteSpace(application.TailoringNotesMarkdown))
        {
            await FailAsync(applicationId, application, "One or more required fields are missing from the stored application", cancellationToken);
            return;
        }

        var cvBaseName = _configuration["GitHub:CvFileName"] ?? "KhawajaBilal_Uddin_CV";
        var coverLetterBaseName = _configuration["GitHub:CoverLetterFileName"] ?? "KhawajaBilal_Uddin_CoverLetter";

        // Generate CV and cover letter docx — exceptions propagate so the consumer can retry.
        // Only permanent state transitions (missing data, PDF failure, GitHub failure) call FailAsync here.
        var cvDocxBytes = await _generationClient.GenerateCvAsync(application, cancellationToken);
        var coverLetterDocxBytes = await _generationClient.GenerateCoverLetterAsync(application, cancellationToken);

        // Convert CV and cover letter docx → PDF — exceptions propagate for consumer retry.
        var cvPdfBytes = await ConvertDocxToPdfAsync(cvDocxBytes, cvBaseName, cancellationToken)
            ?? throw new InvalidOperationException($"LibreOffice produced no output for {cvBaseName}");

        var coverLetterPdfBytes = await ConvertDocxToPdfAsync(coverLetterDocxBytes, coverLetterBaseName, cancellationToken)
            ?? throw new InvalidOperationException($"LibreOffice produced no output for {coverLetterBaseName}");

        // Build the 6-file set for GitHub commit
        var compatibilityReportBytes = System.Text.Encoding.UTF8.GetBytes(application.CompatibilityReportMarkdown);
        var tailoringNotesBytes = System.Text.Encoding.UTF8.GetBytes(application.TailoringNotesMarkdown);

        var files = new List<IngestedFile>
        {
            MakeFile($"{cvBaseName}.docx", cvDocxBytes),
            MakeFile($"{cvBaseName}.pdf", cvPdfBytes),
            MakeFile($"{coverLetterBaseName}.docx", coverLetterDocxBytes),
            MakeFile($"{coverLetterBaseName}.pdf", coverLetterPdfBytes),
            MakeFile("compatibility-report.md", compatibilityReportBytes),
            MakeFile("tailoring-notes.md", tailoringNotesBytes),
        };

        // GitHub commit — propagate failures for consumer retry.
        var ingestResult = await _fileIngestService.IngestAsync(application.CompanyName, files, cancellationToken);
        if (!ingestResult.IsSuccess)
            throw new InvalidOperationException(ingestResult.ErrorMessage ?? "GitHub commit returned failure");

        await _repository.UpdateStatusAsync(
            applicationId,
            status: "Ready to Apply",
            commitUrl: ingestResult.CommitUrl,
            cancellationToken: cancellationToken);

        try
        {
            var jobEvent = new JobApplicationEvent
            {
                ApplicationId = applicationId,
                CompanyName = application.CompanyName,
                JobTitle = application.JobTitle,
                MatchScore = application.MatchScore,
                Recommendation = application.Recommendation,
                Status = "Ready to Apply",
                URL = application.JobUrl,
                EventType = "created",
                Timestamp = DateTime.UtcNow
            };

            await _publisher.PublishJobApplicationEventAsync(jobEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish updated event for {CompanyName}; processing succeeded", application.CompanyName);
        }

        _logger.LogInformation(
            "Successfully processed {CompanyName}: committed {Count} files, commitUrl={Url}",
            application.CompanyName, files.Count, ingestResult.CommitUrl);
    }

    public async Task MarkFailedAsync(string applicationId, string reason, CancellationToken cancellationToken = default)
    {
        var application = await _repository.GetByIdAsync(applicationId, cancellationToken);
        if (application == null)
        {
            _logger.LogWarning("MarkFailedAsync: application not found: {Id}", applicationId);
            return;
        }

        await FailAsync(applicationId, application, reason, cancellationToken);
    }

    private async Task FailAsync(
        string applicationId,
        Domain.Entities.JobApplication application,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Processing failed for {CompanyName} (id={Id}): {Error}",
            application.CompanyName, applicationId, errorMessage);

        await _repository.UpdateStatusAsync(
            applicationId,
            status: "Failed",
            errorDetails: errorMessage,
            cancellationToken: cancellationToken);

        try
        {
            var failedEvent = new JobApplicationEvent
            {
                ApplicationId = applicationId,
                CompanyName = application.CompanyName,
                JobTitle = application.JobTitle,
                MatchScore = application.MatchScore,
                Recommendation = application.Recommendation,
                Status = "Failed",
                URL = application.JobUrl,
                EventType = "updated",
                Timestamp = DateTime.UtcNow
            };

            await _publisher.PublishJobApplicationEventAsync(failedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish failure event for {CompanyName}", application.CompanyName);
        }
    }

    private async Task<byte[]?> ConvertDocxToPdfAsync(byte[] docxBytes, string baseName, CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var docxPath = Path.Combine(tempDir, $"{baseName}.docx");
            await File.WriteAllBytesAsync(docxPath, docxBytes, cancellationToken);

            // Configurable path — override in appsettings.Development.json for local Windows:
            // "LibreOffice": { "ExecutablePath": "C:\\Program Files\\LibreOffice\\program\\soffice.exe" }
            var libreOfficePath = _configuration["LibreOffice:ExecutablePath"] ?? "libreoffice";

            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = libreOfficePath,
                Arguments = $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{docxPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("LibreOffice exited with code {Code} for {Name}: {Stderr}",
                    process.ExitCode, baseName, stderr);
                return null;
            }

            var pdfPath = Path.Combine(tempDir, $"{baseName}.pdf");
            if (!File.Exists(pdfPath))
            {
                _logger.LogError("PDF not found after LibreOffice conversion: {Path}", pdfPath);
                return null;
            }

            _logger.LogInformation("Converted {Name}.docx → PDF ({Bytes} bytes)", baseName, new FileInfo(pdfPath).Length);
            return await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during DOCX to PDF conversion for {Name}", baseName);
            return null;
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    private static IngestedFile MakeFile(string fileName, byte[] bytes) =>
        new()
        {
            FileName = fileName,
            Content = new MemoryStream(bytes),
            Length = bytes.Length
        };
}
