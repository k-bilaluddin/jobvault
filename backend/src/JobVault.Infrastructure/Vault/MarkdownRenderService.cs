using JobVault.Application.Interfaces;
using Markdig;

namespace JobVault.Infrastructure.Vault;

public class MarkdownRenderService : IMarkdownRenderService
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string RenderToHtml(string markdown) => Markdown.ToHtml(markdown, _pipeline);
}
