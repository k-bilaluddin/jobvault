using System.Net.Http.Headers;
using JobVault.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobVault.Infrastructure.Routines;

/// <summary>
/// Fires the claude.ai routine ("Job Queue (API)") that evaluates pending job-queue entries.
/// Calls POST {Routine:BaseUrl}/v1/code/triggers/{Routine:TriggerId}/run with the routine's
/// API token — see docs/env.md for ROUTINE_TRIGGER_TOKEN / ROUTINE_TRIGGER_ID.
/// </summary>
public sealed class ClaudeRoutineTriggerClient : IRoutineTriggerClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeRoutineTriggerClient> _logger;

    public ClaudeRoutineTriggerClient(HttpClient http, IConfiguration configuration, ILogger<ClaudeRoutineTriggerClient> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var token = _configuration["Routine:TriggerToken"];
        var triggerId = _configuration["Routine:TriggerId"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(triggerId))
        {
            throw new InvalidOperationException(
                "Routine:TriggerToken / Routine:TriggerId are not configured (ROUTINE_TRIGGER_TOKEN / ROUTINE_TRIGGER_ID env vars). See docs/env.md.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/v1/code/triggers/{triggerId}/run");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _logger.LogInformation("Firing job-queue routine {TriggerId}", triggerId);
        using var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var message = $"Routine trigger returned {(int)response.StatusCode}: {body}";

            // 4xx = bad request/auth — retrying won't help.
            // 5xx / network errors = transient.
            if ((int)response.StatusCode is >= 400 and < 500)
                throw new InvalidOperationException(message);

            throw new HttpRequestException(message);
        }

        _logger.LogInformation("Job-queue routine fired successfully");
    }
}
