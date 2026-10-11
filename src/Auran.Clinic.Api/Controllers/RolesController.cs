using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/roles")]
[Produces("application/json")]
public sealed class RolesController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Roles.View)]
    [SwaggerOperation(
        Summary = "List protected system roles",
        Description = "Returns the backend-owned system role catalog and effective built-in permission keys. System roles are read-only in V1.",
        OperationId = "Roles_List",
        Tags = new[] { "RBAC" })]
    public ActionResult<BaseResponse<IReadOnlyList<SystemRoleCatalogResponse>>> List()
    {
        var roles = SystemRoleCatalog.All
            .Select(role => new SystemRoleCatalogResponse(
                role.Code,
                role.Name,
                role.Permissions))
            .ToList();

        return Ok(new BaseResponse<IReadOnlyList<SystemRoleCatalogResponse>>
        {
            Status = true,
            Data = roles
        });
    }
}
