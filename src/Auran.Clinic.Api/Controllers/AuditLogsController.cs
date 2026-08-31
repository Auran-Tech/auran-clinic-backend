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
[Route("api/audit-logs")]
[Produces("application/json")]
[Authorize(Policy = PermissionPolicy.ClinicPrefix + Permissions.Clinic.AuditLogs.View)]
public sealed class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet("search")]
    [SwaggerOperation(Summary = "Search current clinic audit logs", Description = "Returns append-only audit history for the authenticated clinic only. Changing ClinicId in the query cannot expand tenant visibility.", OperationId = "AuditLogs_Search", Tags = new[] { "Audit" })]
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

    [HttpGet("get/{id:guid}")]
    [SwaggerOperation(Summary = "Get current clinic audit entry", Description = "Returns one immutable audit record when it belongs to the authenticated clinic.", OperationId = "AuditLogs_GetById", Tags = new[] { "Audit" })]
    public async Task<ActionResult<BaseResponse<AuditLogResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await auditService.GetByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Audit log entry was not found." })
            : Ok(new BaseResponse<AuditLogResponse> { Status = true, Data = result });
    }
}
