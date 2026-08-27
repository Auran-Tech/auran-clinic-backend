using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/platform/clinics")]
[Produces("application/json")]
[Authorize(Policy = ActorPolicies.Platform)]
public sealed class PlatformClinicsController(
    IPlatformClinicService clinicService,
    IValidator<CreateClinicRequest> createValidator,
    IValidator<UpdateClinicRequest> updateValidator,
    IValidator<ClinicSearchRequest> searchValidator,
    IValidator<UpdateClinicFeaturesRequest> featuresValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.Create))]
    [SwaggerOperation(Summary = "Provision a clinic", Description = "Platform-only transactional clinic provisioning. Creates clinic settings, default feature mappings, protected clinic roles, clinic role permissions, the first clinic Admin Identity/domain account and audit history.", OperationId = "PlatformClinics_Create", Tags = new[] { "Platform Clinics" })]
    [ProducesResponseType(typeof(BaseResponse<ClinicDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> Create([FromBody] CreateClinicRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(Error("Validation failed.", validation.Errors.Select(x => x.ErrorMessage)));

        var result = await clinicService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var response = Error(result.Error ?? "Clinic provisioning failed.");
            return result.IsConflict ? Conflict(response) : BadRequest(response);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Clinic!.Id }, new BaseResponse<ClinicDetailsResponse>
        {
            Status = true,
            Message = "Clinic provisioned successfully.",
            Data = result.Clinic
        });
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.View))]
    [SwaggerOperation(Summary = "Search clinics", Description = "Platform-only paginated clinic search across tenants. Supports text and active-state filters and does not expose clinical patient data.", OperationId = "PlatformClinics_Search", Tags = new[] { "Platform Clinics" })]
    [ProducesResponseType(typeof(BaseResponse<PaginatedResponse<ClinicSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<ClinicSummaryResponse>>>> Search([FromQuery] ClinicSearchRequest request, CancellationToken cancellationToken)
    {
        var validation = await searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(Error("Validation failed.", validation.Errors.Select(x => x.ErrorMessage)));
        return Ok(new BaseResponse<PaginatedResponse<ClinicSummaryResponse>> { Status = true, Data = await clinicService.SearchAsync(request, cancellationToken) });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.View))]
    [SwaggerOperation(Summary = "Get clinic administration details", Description = "Returns tenant identity, configuration and initial Admin summary for platform administration. It does not expose patient or clinical records.", OperationId = "PlatformClinics_GetById", Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await clinicService.GetByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Clinic was not found." })
            : Ok(new BaseResponse<ClinicDetailsResponse> { Status = true, Data = result });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.Update))]
    [SwaggerOperation(Summary = "Update clinic administration data", Description = "Updates platform-managed clinic identity and branding data. Every mutation is audited.", OperationId = "PlatformClinics_Update", Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse>> Update(Guid id, [FromBody] UpdateClinicRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(Error("Validation failed.", validation.Errors.Select(x => x.ErrorMessage)));
        try
        {
            return await clinicService.UpdateAsync(id, request, cancellationToken)
                ? Ok(new BaseResponse { Status = true, Message = "Clinic updated successfully." })
                : NotFound(new BaseResponse { Status = false, Message = "Clinic was not found." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Error(ex.Message));
        }
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.SetStatus))]
    [SwaggerOperation(Summary = "Set clinic status", Description = "Activates or suspends a clinic. Suspension is enforced on subsequent clinic requests, including requests using previously issued JWTs, through the centralized clinic access guard.", OperationId = "PlatformClinics_SetStatus", Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse>> SetStatus(Guid id, [FromBody] SetClinicStatusRequest request, CancellationToken cancellationToken)
    {
        return await clinicService.SetActiveAsync(id, request.IsActive, cancellationToken)
            ? Ok(new BaseResponse { Status = true, Message = request.IsActive ? "Clinic activated." : "Clinic suspended." })
            : NotFound(new BaseResponse { Status = false, Message = "Clinic was not found." });
    }

    [HttpGet("{id:guid}/features")]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.View))]
    [SwaggerOperation(Summary = "Get clinic features", Description = "Returns the global feature catalog projected with enabled state and per-clinic configuration for the selected clinic.", OperationId = "PlatformClinicFeatures_Get", Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<ClinicFeatureResponse>>>> GetFeatures(Guid id, CancellationToken cancellationToken)
    {
        var result = await clinicService.GetFeaturesAsync(id, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Clinic was not found." })
            : Ok(new BaseResponse<IReadOnlyCollection<ClinicFeatureResponse>> { Status = true, Data = result });
    }

    [HttpPut("{id:guid}/features")]
    [Authorize(Policy = PermissionPolicy.ForPlatform(Permissions.Platform.Clinics.ManageFeatures))]
    [SwaggerOperation(Summary = "Update clinic features", Description = "Platform-only feature entitlement management. Feature state is independent from clinic RBAC permissions and cache entries are invalidated immediately after changes.", OperationId = "PlatformClinicFeatures_Update", Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<ClinicFeatureResponse>>>> UpdateFeatures(Guid id, [FromBody] UpdateClinicFeaturesRequest request, CancellationToken cancellationToken)
    {
        var validation = await featuresValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(Error("Validation failed.", validation.Errors.Select(x => x.ErrorMessage)));
        var result = await clinicService.UpdateFeaturesAsync(id, request, cancellationToken);
        return result is null
            ? NotFound(new BaseResponse { Status = false, Message = "Clinic was not found." })
            : Ok(new BaseResponse<IReadOnlyCollection<ClinicFeatureResponse>> { Status = true, Data = result });
    }

    private static BaseResponse Error(string message, IEnumerable<string>? errors = null) => new()
    {
        Status = false,
        Message = message,
        Error = errors is null ? message : string.Join(" ", errors)
    };
}
