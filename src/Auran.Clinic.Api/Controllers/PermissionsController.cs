using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Produces("application/json")]
public sealed class PermissionsController(IPermissionCatalogService permissionCatalogService) : ControllerBase
{
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.RbacView)]
    [HttpGet("list")]
    [SwaggerOperation(
        Summary = "List the system permission catalog",
        Description = "Returns stable permission keys, group keys and all available localized descriptions. Super Users satisfy this permission automatically.",
        OperationId = "Permissions_List",
        Tags = new[] { "Authorization" })]
    [ProducesResponseType(typeof(BaseResponse<List<PermissionCatalogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<List<PermissionCatalogResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var permissions = await permissionCatalogService.GetAllAsync(cancellationToken);
        return Ok(new BaseResponse<List<PermissionCatalogResponse>>
        {
            Status = true,
            Data = permissions.ToList()
        });
    }
}
