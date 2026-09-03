using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/permissions")]
[Produces("application/json")]
public sealed class PermissionsController(IPermissionCatalogService permissionCatalogService) : ControllerBase
{
    [HttpGet("list")]
    [SwaggerOperation(
        Summary = "List the permission catalog",
        Description = "Returns stable backend permission keys, permission groups, and all localized descriptions currently stored for each permission.",
        OperationId = "Permissions_List",
        Tags = new[] { "Permissions" })]
    [ProducesResponseType(typeof(BaseResponse<List<PermissionCatalogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<List<PermissionCatalogResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var permissions = await permissionCatalogService.GetAsync(cancellationToken);
        return Ok(new BaseResponse<List<PermissionCatalogResponse>>
        {
            Status = true,
            Data = permissions
        });
    }
}
