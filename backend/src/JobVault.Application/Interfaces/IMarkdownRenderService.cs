namespace JobVault.Application.Interfaces;

public interface IMarkdownRenderService
{
    string RenderToHtml(string markdown);
}
