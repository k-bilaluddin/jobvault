using JobVault.Domain.ValueObjects;

namespace JobVault.Contracts.Responses;

public class ContentResponse
{
    public string Headline { get; init; } = "";
    public string Summary { get; init; } = "";
    public List<SkillRow> Skills { get; init; } = [];
    public List<RolePayload> Roles { get; init; } = [];
    public string Recipient { get; init; } = "";
    public List<string> CoverLetterParagraphs { get; init; } = [];
    public List<string> Strengths { get; init; } = [];
    public List<string> Gaps { get; init; } = [];
}
