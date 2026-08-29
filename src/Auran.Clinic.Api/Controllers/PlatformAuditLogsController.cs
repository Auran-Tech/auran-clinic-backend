using Auran.Clinic.Api.Contracts.Auditing;
using Auran.Clinic.Api.Mappings;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/platform/audit-logs")]
[Produces("application/json")]
[Authorize(Policy = PermissionPolicy.PlatformPrefix + Permissions.Platform.AuditLogs.View)]
public sealed class PlatformAuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Search platform audit logs", Description = "Returns platform-scope events plus clinic-management events performed by platform actors. It intentionally excludes clinic-user clinical activity.", OperationId = "PlatformAuditLogs_Search", Tags = new[] { "Platform Audit" })]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<AuditLogResponse>>>> Search(
        [FromQuery] AuditLogSearchApiRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(new BaseResponse<PaginatedResponse<AuditLogResponse>>
        {
            Status = true,
            Data = await auditService.SearchAsync(request.ToServiceRequest(), cancellationToken)
        });
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get platform-visible audit entry", Description = "Returns one audit record when it is within platform administrative audit visibility.", OperationId = "PlatformAuditLogs_GetById", Tags = new[] { "Platform Audit" })]
    public async Task<ActionResult<BaseResponse<AuditLogResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await auditService.GetByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Audit log entry was not found." })
            : Ok(new BaseResponse<AuditLogResponse> { Status = true, Data = result });
    }
}
