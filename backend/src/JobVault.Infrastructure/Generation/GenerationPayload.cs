using JobVault.Domain.ValueObjects;

namespace JobVault.Infrastructure.Generation;

internal sealed class GenerationPayload
{
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? JdSource { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<SkillRow> Skills { get; set; } = [];
    public List<RolePayload> Roles { get; set; } = [];
    public string? Recipient { get; set; }
    public List<string> CoverLetterParagraphs { get; set; } = [];
    public int CompatibilityScore { get; set; }
    public List<string> Strengths { get; set; } = [];
    public List<string> Gaps { get; set; } = [];
    public string? TailoringNotes { get; set; }
}
