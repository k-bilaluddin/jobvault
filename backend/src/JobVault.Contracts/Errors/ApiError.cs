using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.Contracts.Errors;

public record ApiError(int Status, string Title, string DetailTemplate)
{
    public ProblemDetails ToProblemDetails(HttpContext context, params object[] args)
    {
        var code = ErrorCatalog.GetCode(this);
        return new ProblemDetails
        {
            Type = $"https://api.kbilaluddin.dev/errors/{code}",
            Title = Title,
            Status = Status,
            Detail = args.Length > 0 ? string.Format(DetailTemplate, args) : DetailTemplate,
            Instance = context.Request.Path,
            Extensions =
            {
                ["code"] = code,
                ["traceId"] = context.TraceIdentifier
            }
        };
    }
}
