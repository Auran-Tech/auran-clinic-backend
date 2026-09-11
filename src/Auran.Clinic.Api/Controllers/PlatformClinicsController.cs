using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Platform)]
[Route("api/platform/clinics")]
[Produces("application/json")]
public sealed class PlatformClinicsController(
    IPlatformClinicService clinicService,
    IValidator<UpdateClinicRequest> updateClinicValidator,
    IValidator<SetClinicStatusRequest> setClinicStatusValidator) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Provision a clinic",
        Description = "Atomically creates the clinic, default settings, a server-generated clinic code, and the initial active Clinic Super User assigned to the protected ADMIN role.",
        OperationId = "PlatformClinics_Create",
        Tags = new[] { "Platform Clinics" })]
    [ProducesResponseType(typeof(BaseResponse<ClinicDetailsResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> Create(
        [FromBody] CreateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await clinicService.CreateAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            var response = new BaseResponse
            {
                Status = false,
                Message = result.Error ?? string.Empty,
                Error = result.IsConflict ? "clinic_provisioning_conflict" : "clinic_provisioning_failed"
            };

            return result.IsConflict ? Conflict(response) : BadRequest(response);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { clinicId = result.Clinic!.Id },
            new BaseResponse<ClinicDetailsResponse>
            {
                Status = true,
                Data = result.Clinic
            });
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "List clinics",
        Description = "Returns the platform-visible clinic catalog without exposing clinic patient or clinical data.",
        OperationId = "PlatformClinics_List",
        Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<ClinicSummaryResponse>>>> List(
        CancellationToken cancellationToken)
    {
        var clinics = await clinicService.ListAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<ClinicSummaryResponse>>
        {
            Status = true,
            Data = clinics
        });
    }

    [HttpGet("details")]
    [SwaggerOperation(
        Summary = "Get clinic",
        Description = "Returns platform-operational clinic metadata and the initial administrator reference. The clinic identifier is supplied as a query parameter.",
        OperationId = "PlatformClinics_Get",
        Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> GetById(
        [FromQuery] Guid clinicId,
        CancellationToken cancellationToken)
    {
        if (clinicId == Guid.Empty)
            return ValidationFailure();

        var clinic = await clinicService.GetAsync(clinicId, cancellationToken);
        if (clinic is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic not found." });

        return Ok(new BaseResponse<ClinicDetailsResponse> { Status = true, Data = clinic });
    }

    [HttpPut]
    [SwaggerOperation(
        Summary = "Update clinic metadata",
        Description = "Updates platform-managed clinic metadata. The request body includes the clinic identifier. The generated clinic code is immutable.",
        OperationId = "PlatformClinics_Update",
        Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse<ClinicDetailsResponse>>> Update(
        [FromBody] UpdateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateClinicValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        var clinic = await clinicService.UpdateAsync(request.ClinicId, request, cancellationToken);
        if (clinic is null)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic not found." });

        return Ok(new BaseResponse<ClinicDetailsResponse> { Status = true, Data = clinic });
    }

    [HttpPut("status")]
    [SwaggerOperation(
        Summary = "Activate or suspend a clinic",
        Description = "Changes the clinic business state. The request body includes the clinic identifier. Suspended clinics immediately fail clinic access-token state validation.",
        OperationId = "PlatformClinics_SetStatus",
        Tags = new[] { "Platform Clinics" })]
    public async Task<ActionResult<BaseResponse>> SetStatus(
        [FromBody] SetClinicStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await setClinicStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ValidationFailure();

        var updated = await clinicService.SetActiveAsync(request.ClinicId, request.IsActive, cancellationToken);
        if (!updated)
            return NotFound(new BaseResponse { Status = false, Message = "Clinic not found." });

        return Ok(new BaseResponse
        {
            Status = true,
            Message = request.IsActive ? "Clinic activated." : "Clinic suspended."
        });
    }

    private BadRequestObjectResult ValidationFailure() => BadRequest(new BaseResponse
    {
        Status = false,
        Error = "validation_error"
    });
}
