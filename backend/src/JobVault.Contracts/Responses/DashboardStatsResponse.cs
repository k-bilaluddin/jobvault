namespace JobVault.Contracts.Responses;

public class DashboardStatsResponse
{
    public int TotalApplications { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public double AverageScore { get; set; }
}