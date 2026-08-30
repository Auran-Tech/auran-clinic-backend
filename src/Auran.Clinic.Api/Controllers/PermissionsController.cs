using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/permissions")]
[Produces("application/json")]
public sealed class PermissionsController(IPermissionCatalogService permissionCatalogService) : ControllerBase
{
    [HttpGet("list")]
    [SwaggerOperation(
        Summary = "Get the permission catalog",
        Description = "Returns stable permission keys with all stored localized descriptions. Clinic actors receive clinic permissions and platform actors receive platform permissions.",
        OperationId = "Permissions_List",
        Tags = new[] { "Permissions" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<PermissionCatalogResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var permissions = await permissionCatalogService.GetAllAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<PermissionCatalogResponse>>
        {
            Status = true,
            Data = permissions
        });
    }
}
