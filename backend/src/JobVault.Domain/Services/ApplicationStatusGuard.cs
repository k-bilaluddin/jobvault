namespace JobVault.Domain.Services;

public enum IngestionMatchAction
{
    /// <summary>No human has acted on the matched application yet — safe for automation to update it in place.</summary>
    UpdateInPlace,

    /// <summary>The matched application is actively engaged with (generated, being regenerated, or under
    /// manual review) — nothing to gain from a duplicate, real risk of clobbering if updated. Do nothing.</summary>
    NoOp,

    /// <summary>The matched application reflects a real-world outcome or explicit human decision — treat
    /// the incoming ingestion as a genuinely new attempt and leave the matched one untouched.</summary>
    CreateNew,
}

/// <summary>
/// Decides what an ingestion match (by normalized URL or company+title fallback) should do with the
/// matched application, based on its current status. See issue #104 for the full design rationale.
/// </summary>
public static class ApplicationStatusGuard
{
    private static readonly HashSet<string> PipelineOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "Processing", "Queued", "Failed",
    };

    private static readonly HashSet<string> ActivelyEngaged = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ready to Apply", "Regenerating", "Researching",
    };

    public static IngestionMatchAction Resolve(string? status)
    {
        if (status != null && PipelineOnly.Contains(status))
            return IngestionMatchAction.UpdateInPlace;

        if (status != null && ActivelyEngaged.Contains(status))
            return IngestionMatchAction.NoOp;

        // Everything else (Applied, Rejected, Interview, Offer, Not Interested, Archived, or any
        // unrecognized status) reflects a real-world outcome or explicit human decision.
        return IngestionMatchAction.CreateNew;
    }
}
