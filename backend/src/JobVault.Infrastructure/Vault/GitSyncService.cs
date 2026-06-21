using System.Diagnostics;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Responses;
using Microsoft.Extensions.Configuration;

namespace JobVault.Infrastructure.Vault;

public class GitSyncService : IGitSyncService
{
    private readonly string? _rootDir;

    public GitSyncService(IConfiguration configuration)
    {
        _rootDir = configuration["Vault:RootDir"];
    }

    public GitSyncResponse Sync()
    {
        if (string.IsNullOrEmpty(_rootDir))
            return new GitSyncResponse { Ok = false, Message = "Vault root directory not configured" };

        if (!Directory.Exists(_rootDir))
            return new GitSyncResponse { Ok = false, Message = "Vault directory not found" };

        try
        {
            var psi = new ProcessStartInfo("git", "pull origin master")
            {
                WorkingDirectory = _rootDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
                return new GitSyncResponse { Ok = false, Message = "Failed to start git process" };

            process.WaitForExit(30000);

            var stdout = process.StandardOutput.ReadToEnd().Trim();
            var stderr = process.StandardError.ReadToEnd().Trim();

            return process.ExitCode == 0
                ? new GitSyncResponse { Ok = true, Message = string.IsNullOrEmpty(stdout) ? "Already up to date." : stdout }
                : new GitSyncResponse { Ok = false, Message = stderr };
        }
        catch (Exception ex)
        {
            return new GitSyncResponse { Ok = false, Message = ex.Message };
        }
    }
}
