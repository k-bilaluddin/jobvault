using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Models.Requests;

public class GetApplicationsRequest
{
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 10;

    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    [FromQuery(Name = "company")]
    public string? Company { get; set; }

    [FromQuery(Name = "fromDate")]
    public DateTime? FromDate { get; set; }

    [FromQuery(Name = "toDate")]
    public DateTime? ToDate { get; set; }

    [FromQuery(Name = "minScore")]
    public int? MinScore { get; set; }

    [FromQuery(Name = "maxScore")]
    public int? MaxScore { get; set; }
}