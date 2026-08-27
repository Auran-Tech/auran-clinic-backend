using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = PermissionPolicy.For(Permissions.AuditLogs.View))]
public sealed class AuditLogsController(
    IAuditService auditService,
    IValidator<AuditLogSearchRequest> searchValidator) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Search audit logs",
        Description = "Returns the append-only audit trail with pagination and filters for clinic, actor, action, category, entity and UTC date range. Ordinary users are always restricted to their authenticated clinic. The ClinicId filter is effective only for Super Users.",
        OperationId = "AuditLogs_Search")]
    [ProducesResponseType(typeof(BaseResponse<PaginatedResponse<AuditLogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<AuditLogResponse>>>> Search(
        [FromQuery] AuditLogSearchRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage));
            return BadRequest(new BaseResponse { Status = false, Message = "Validation failed.", Error = error });
        }

        var result = await auditService.SearchAsync(request, cancellationToken);
        return Ok(new BaseResponse<PaginatedResponse<AuditLogResponse>> { Status = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get audit log entry",
        Description = "Returns one immutable audit record. Tenant isolation prevents an ordinary user from resolving an audit entry that belongs to another clinic.",
        OperationId = "AuditLogs_GetById")]
    [ProducesResponseType(typeof(BaseResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<AuditLogResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await auditService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Audit log entry was not found or is outside the current tenant." });

        return Ok(new BaseResponse<AuditLogResponse> { Status = true, Data = result });
    }
}
