using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Patients;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/patient-clinical-data")]
[Produces("application/json")]
public sealed class PatientClinicalDataController(
    IPatientDynamicProfileService dynamicProfileService,
    IPatientMeasurementService measurementService,
    IValidator<PatientLookupRequest> lookupValidator,
    IValidator<SavePatientDynamicValueRequest> dynamicValueValidator,
    IValidator<AddClinicalMeasurementRequest> measurementValidator) : ControllerBase
{
    [HttpPost("dynamic-profile/details")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.View)]
    public async Task<ActionResult<BaseResponse<PatientDynamicProfileResponse>>> GetDynamicProfile(
        [FromBody] PatientLookupRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await lookupValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<PatientDynamicProfileResponse> { Status = false, Error = "validation_error" });

        var result = await dynamicProfileService.GetAsync(request.PatientId, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return Ok(new BaseResponse<PatientDynamicProfileResponse> { Status = true, Data = result });
    }

    [HttpPut("dynamic-profile/value")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.Edit)]
    public async Task<ActionResult<BaseResponse>> SaveDynamicValue(
        [FromBody] SavePatientDynamicValueRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await dynamicValueValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        var saved = await dynamicProfileService.SaveValueAsync(request, cancellationToken);
        if (!saved)
            return NotFound(new BaseResponse { Status = false, Message = "Patient or profile field not found." });

        return Ok(new BaseResponse { Status = true });
    }

    [HttpPost("measurements/details")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.View)]
    public async Task<ActionResult<BaseResponse<PatientMeasurementsResponse>>> GetMeasurements(
        [FromBody] PatientLookupRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await lookupValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<PatientMeasurementsResponse> { Status = false, Error = "validation_error" });

        var result = await measurementService.GetAsync(request.PatientId, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient not found." });

        return Ok(new BaseResponse<PatientMeasurementsResponse> { Status = true, Data = result });
    }

    [HttpPost("measurements")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.MedicalProfile.Edit)]
    public async Task<ActionResult<BaseResponse<ClinicalMeasurementResponse>>> AddMeasurement(
        [FromBody] AddClinicalMeasurementRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await measurementValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<ClinicalMeasurementResponse> { Status = false, Error = "validation_error" });

        var result = await measurementService.AddAsync(request, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Patient or clinical field not found." });

        return StatusCode(StatusCodes.Status201Created, new BaseResponse<ClinicalMeasurementResponse>
        {
            Status = true,
            Data = result
        });
    }
}
