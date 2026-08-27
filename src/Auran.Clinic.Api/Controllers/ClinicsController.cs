using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/clinics")]
[Authorize]
public sealed class ClinicsController(
    IClinicService clinicService,
    IValidator<CreateClinicRequest> createValidator,
    IValidator<UpdateClinicRequest> updateValidator,
    IValidator<UpdateClinicSettingsRequest> settingsValidator,
    IValidator<ClinicSearchRequest> searchValidator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.Create))]
    [SwaggerOperation(
        Summary = "Provision a clinic",
        Description = "Creates a clinic as one transactional provisioning operation. The operation creates default clinic settings, ensures the global permission catalog, creates the protected Admin, Receptionist, Doctor and Nurse roles, assigns default role permissions, creates the initial ASP.NET Core Identity admin account and linked domain user, assigns the Admin role, and writes audit records. Only a Super User can successfully provision a clinic.",
        OperationId = "Clinics_Create")]
    [ProducesResponseType(typeof(BaseResponse<ClinicDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> Create(
        [FromBody] CreateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ValidationError(validation.Errors.Select(x => x.ErrorMessage)));

        var result = await clinicService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var error = new BaseResponse { Status = false, Message = result.Error, Error = result.Error };
            return result.IsConflict ? Conflict(error) : BadRequest(error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Clinic!.Id }, new BaseResponse<ClinicDetailsResponse>
        {
            Status = true,
            Message = "Clinic provisioned successfully.",
            Data = result.Clinic
        });
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.View))]
    [SwaggerOperation(
        Summary = "Search clinics",
        Description = "Returns paginated clinics. Super Users can search all clinics; ordinary clinic users are tenant-isolated and can only receive their own clinic. Supports text search and active-state filtering.",
        OperationId = "Clinics_Search")]
    [ProducesResponseType(typeof(BaseResponse<PaginatedResponse<ClinicSummaryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<PaginatedResponse<ClinicSummaryResponse>>>> Search(
        [FromQuery] ClinicSearchRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ValidationError(validation.Errors.Select(x => x.ErrorMessage)));

        var result = await clinicService.SearchAsync(request, cancellationToken);
        return Ok(new BaseResponse<PaginatedResponse<ClinicSummaryResponse>> { Status = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.View))]
    [SwaggerOperation(
        Summary = "Get clinic details",
        Description = "Returns clinic identity, branding, configuration and initial Admin summary. Tenant isolation is enforced by the service; non-Super Users cannot read another clinic by changing the route id.",
        OperationId = "Clinics_GetById")]
    [ProducesResponseType(typeof(BaseResponse<ClinicDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicService.GetByIdAsync(id, cancellationToken);
        if (clinic is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic was not found or is outside the current tenant." });

        return Ok(new BaseResponse<ClinicDetailsResponse> { Status = true, Data = clinic });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.Update))]
    [SwaggerOperation(
        Summary = "Update clinic",
        Description = "Updates clinic identity and branding fields. Clinic code uniqueness is enforced globally and every changed entity value is captured by the central audit interceptor.",
        OperationId = "Clinics_Update")]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse>> Update(
        Guid id,
        [FromBody] UpdateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ValidationError(validation.Errors.Select(x => x.ErrorMessage)));

        try
        {
            var updated = await clinicService.UpdateAsync(id, request, cancellationToken);
            if (!updated)
                return NotFound(new BaseResponse { Status = false, Message = "Clinic was not found or is outside the current tenant." });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new BaseResponse { Status = false, Message = exception.Message, Error = exception.Message });
        }

        return Ok(new BaseResponse { Status = true, Message = "Clinic updated successfully." });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.Activate))]
    [SwaggerOperation(
        Summary = "Deactivate clinic",
        Description = "Soft-deactivates the clinic. No clinic or medical history row is physically deleted. New login and refresh sessions are blocked for inactive clinics, while the audit trail records the state change.",
        OperationId = "Clinics_Deactivate")]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var updated = await clinicService.SetActiveAsync(id, false, cancellationToken);
        if (!updated)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic was not found or is outside the current tenant." });

        return Ok(new BaseResponse { Status = true, Message = "Clinic deactivated successfully." });
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.Activate))]
    [SwaggerOperation(
        Summary = "Activate clinic",
        Description = "Reactivates a previously deactivated clinic and records the action in the append-only audit trail.",
        OperationId = "Clinics_Activate")]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var updated = await clinicService.SetActiveAsync(id, true, cancellationToken);
        if (!updated)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic was not found or is outside the current tenant." });

        return Ok(new BaseResponse { Status = true, Message = "Clinic activated successfully." });
    }

    [HttpGet("{id:guid}/settings")]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.SettingsView))]
    [SwaggerOperation(
        Summary = "Get clinic settings",
        Description = "Returns tenant-specific contact, locale, date/time, prescription, documentation reminder and welcome-page settings for the requested clinic.",
        OperationId = "ClinicSettings_Get")]
    [ProducesResponseType(typeof(BaseResponse<ClinicSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> GetSettings(
        Guid id,
        CancellationToken cancellationToken)
    {
        var settings = await clinicService.GetSettingsAsync(id, cancellationToken);
        if (settings is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic settings were not found or are outside the current tenant." });

        return Ok(new BaseResponse<ClinicSettingsResponse> { Status = true, Data = settings });
    }

    [HttpPut("{id:guid}/settings")]
    [Authorize(Policy = PermissionPolicy.For(Permissions.Clinics.SettingsUpdate))]
    [SwaggerOperation(
        Summary = "Update clinic settings",
        Description = "Updates tenant-specific clinic settings. The central audit interceptor captures changed fields and old/new values, with secret redaction applied automatically.",
        OperationId = "ClinicSettings_Update")]
    [ProducesResponseType(typeof(BaseResponse<ClinicSettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BaseResponse<ClinicSettingsResponse>>> UpdateSettings(
        Guid id,
        [FromBody] UpdateClinicSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await settingsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ValidationError(validation.Errors.Select(x => x.ErrorMessage)));

        var settings = await clinicService.UpdateSettingsAsync(id, request, cancellationToken);
        if (settings is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic was not found or is outside the current tenant." });

        return Ok(new BaseResponse<ClinicSettingsResponse>
        {
            Status = true,
            Message = "Clinic settings updated successfully.",
            Data = settings
        });
    }

    private static BaseResponse ValidationError(IEnumerable<string> errors)
    {
        var error = string.Join(" ", errors);
        return new BaseResponse { Status = false, Message = "Validation failed.", Error = error };
    }
}
