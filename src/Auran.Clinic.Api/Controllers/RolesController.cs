using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/roles")]
[Produces("application/json")]
public sealed class RolesController(IRoleCatalogService roleCatalogService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Roles.View)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<RoleCatalogResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var roles = await roleCatalogService.GetAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyCollection<RoleCatalogResponse>>
        {
            Status = true,
            Data = roles
        });
    }
}
