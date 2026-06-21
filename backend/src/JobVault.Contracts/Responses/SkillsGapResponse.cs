namespace JobVault.Contracts.Responses;

public class SkillsGapResponse
{
    public List<SkillGapEntry> Gaps { get; init; } = [];
    public int Reports_scanned { get; init; }
}

public class SkillGapEntry
{
    public string Skill { get; init; } = "";
    public int Count { get; init; }
    public List<string> Companies { get; init; } = [];
}
