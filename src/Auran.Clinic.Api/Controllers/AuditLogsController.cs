using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Authorize(Policy = PermissionPolicy.Prefix + Permissions.Audit.View)]
[Route("api/audit-logs")]
[Produces("application/json")]
public sealed class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Read recent clinic audit events",
        Description = "Returns the most recent audit events visible to the authenticated clinic. Results are tenant-scoped and capped at 200 rows per request.",
        OperationId = "Audit_GetRecent",
        Tags = new[] { "Audit" })]
    [ProducesResponseType(typeof(BaseResponse<IReadOnlyList<AuditLogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<AuditLogResponse>>>> GetRecent(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var logs = await auditService.GetRecentAsync(take, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<AuditLogResponse>>
        {
            Status = true,
            Data = logs
        });
    }
}
