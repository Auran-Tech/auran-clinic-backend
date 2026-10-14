using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.DocumentationPolicies;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Infrastructure.DocumentationPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/documentation-policies")]
[Produces("application/json")]
public sealed class DocumentationPoliciesController(IDocumentationPolicyService service) : ControllerBase
{
    [HttpGet("{specialtyCode}")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(Summary = "Get clinic specialty documentation requirements",
        OperationId = "DocumentationPolicies_Get", Tags = new[] { "Clinical Documentation" })]
    public async Task<ActionResult<BaseResponse<DocumentationPolicyResponse>>> Get(
        string specialtyCode, CancellationToken ct)
    {
        var code = DocumentationPolicyService.NormalizeSpecialty(specialtyCode);
        if (!DocumentationPolicyService.IsValidSpecialty(code))
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });
        var policy = await service.GetAsync(code, ct);
        if (policy is null)
            return NotFound(new BaseResponse { Status = false, Error = "policy_not_configured" });
        return Ok(new BaseResponse<DocumentationPolicyResponse> { Status = true, Data = policy });
    }

    [HttpPut("{specialtyCode}")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    [SwaggerOperation(Summary = "Configure required documentation fields for a clinic specialty",
        OperationId = "DocumentationPolicies_Update", Tags = new[] { "Clinical Documentation" })]
    public async Task<ActionResult<BaseResponse<DocumentationPolicyResponse>>> Update(
        string specialtyCode, [FromBody] UpdateDocumentationPolicyRequest request, CancellationToken ct)
    {
        var code = DocumentationPolicyService.NormalizeSpecialty(specialtyCode);
        if (!DocumentationPolicyService.IsValidSpecialty(code) ||
            request.RequiredFields is not { Length: > 0 } fields ||
            fields.Length > DocumentationPolicyService.Fields.Count ||
            fields.Distinct(StringComparer.Ordinal).Count() != fields.Length ||
            fields.Any(field => !DocumentationPolicyService.Fields.Contains(field)))
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        var result = await service.UpdateAsync(code, fields, ct);
        return Ok(new BaseResponse<DocumentationPolicyResponse> { Status = true, Data = result });
    }

    [HttpPut("doctors/{doctorId:guid}")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    [SwaggerOperation(Summary = "Assign a doctor's documentation specialty",
        OperationId = "DocumentationPolicies_AssignDoctor", Tags = new[] { "Clinical Documentation" })]
    public async Task<ActionResult<BaseResponse>> AssignDoctor(
        Guid doctorId, [FromBody] AssignDoctorSpecialtyRequest request, CancellationToken ct)
    {
        var code = DocumentationPolicyService.NormalizeSpecialty(request.SpecialtyCode);
        if (!DocumentationPolicyService.IsValidSpecialty(code))
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });
        if (!await service.AssignDoctorAsync(doctorId, code, ct))
            return NotFound(new BaseResponse { Status = false, Error = "doctor_not_found" });
        return Ok(new BaseResponse { Status = true, Message = "Documentation specialty assigned." });
    }
}
