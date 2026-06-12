using JobVault.Application.Common;
using JobVault.Contracts.External.GitHub;

namespace JobVault.Application.Interfaces;

/// <summary>
/// Handles webhook payloads from external sources.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Processes a GitHub webhook payload.
    /// </summary>
    /// <param name="payload">The webhook payload to process.</param>
    /// <returns>The result of the webhook processing.</returns>
    Task<WebhookResult> HandleAsync(GitHubWebhookPayload payload);
}