using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/platform/audit-logs")]
[Produces("application/json")]
[Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.AuditLogs.View))]
public sealed class PlatformAuditLogsController(IAuditService auditService, IValidator<AuditLogSearchRequest> validator) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Search platform audit logs", Description = "Returns platform-scope events plus clinic-management events performed by platform actors. It intentionally excludes clinic-user clinical activity.", OperationId = "PlatformAuditLogs_Search", Tags = new[] { "Platform Audit" })]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<AuditLogResponse>>>> Search([FromQuery] AuditLogSearchRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Message = "Validation failed.", Error = string.Join(" ", validation.Errors.Select(x => x.ErrorMessage)) });
        return Ok(new BaseResponse<PaginatedResponse<AuditLogResponse>> { Status = true, Data = await auditService.SearchAsync(request, cancellationToken) });
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
