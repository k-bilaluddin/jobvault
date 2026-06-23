using JobVault.Contracts.Errors;
using Microsoft.AspNetCore.Mvc;

namespace JobVault.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ErrorResponse(string code, params object[] args)
    {
        var problem = ErrorCatalog.ToProblem(code, HttpContext, args);
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
