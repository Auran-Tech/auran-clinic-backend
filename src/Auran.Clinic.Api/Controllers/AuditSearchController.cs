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
[Route("api/audit-search")]
[Produces("application/json")]
public sealed class AuditSearchController(IAuditReadService auditReadService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Search clinic audit events",
        Description = "Searches tenant-scoped audit events by action, entity type, actor, and UTC time range. Results are capped at 200 rows.",
        OperationId = "Audit_Search",
        Tags = new[] { "Audit" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<AuditLogListItemResponse>>>> Search(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var logs = await auditReadService.SearchAsync(query, cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<AuditLogListItemResponse>>
        {
            Status = true,
            Data = logs
        });
    }
}
