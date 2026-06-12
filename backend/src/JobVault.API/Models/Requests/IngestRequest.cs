using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Models.Requests;

public class IngestRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public List<IFormFile> Files { get; set; } = new();
}