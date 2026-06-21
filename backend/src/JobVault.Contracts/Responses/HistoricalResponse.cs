namespace JobVault.Contracts.Responses;

public class HistoricalResponse
{
    public bool Available { get; init; }
    public List<HistoricalEntry> Historical { get; init; } = [];
    public List<CurrentEntry> Current { get; init; } = [];
    public string Scanned_at { get; init; } = "";
}

public class HistoricalEntry
{
    public string Name { get; init; } = "";
    public string Applied_date { get; init; } = "";
    public string Stage { get; init; } = "";
    public string Source { get; init; } = "";
}

public class CurrentEntry
{
    public string Name { get; init; } = "";
    public string Applied_date { get; init; } = "";
    public string Stage { get; init; } = "";
    public string Source { get; init; } = "";
    public bool Current { get; init; } = true;
}
