using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.ClinicalMeasurements;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/clinical-measurements")]
[Produces("application/json")]
public sealed class ClinicalMeasurementsController(
    IClinicalMeasurementService service,
    IValidator<RecordClinicalMeasurementsRequest> recordValidator) : ControllerBase
{
    [HttpGet("fields")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "List enabled clinical measurement fields",
        Description = "Returns tenant-scoped configured clinical fields and select options.",
        OperationId = "ClinicalMeasurements_ListFields",
        Tags = new[] { "Clinical Measurements" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<ClinicalMeasurementFieldResponse>>>> Fields(
        CancellationToken cancellationToken)
    {
        var data = await service.ListFieldsAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<ClinicalMeasurementFieldResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "List visit clinical measurements",
        Description = "Returns historical configured measurements recorded for a visit.",
        OperationId = "ClinicalMeasurements_ListForVisit",
        Tags = new[] { "Clinical Measurements" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<ClinicalMeasurementResponse>>>> List(
        [FromQuery] Guid visitId,
        CancellationToken cancellationToken)
    {
        if (visitId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<IReadOnlyList<ClinicalMeasurementResponse>>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var data = await service.ListForVisitAsync(visitId, cancellationToken);
        if (data is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            });
        }

        return Ok(new BaseResponse<IReadOnlyList<ClinicalMeasurementResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Record clinical measurements",
        Description = "Appends a validated batch of configured typed clinical measurements to an open visit. Assigned doctor or Clinic Super User only.",
        OperationId = "ClinicalMeasurements_Record",
        Tags = new[] { "Clinical Measurements" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<ClinicalMeasurementResponse>>>> Record(
        [FromBody] RecordClinicalMeasurementsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await recordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<IReadOnlyList<ClinicalMeasurementResponse>>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await service.RecordAsync(request, cancellationToken);
        return result.Outcome switch
        {
            ClinicalMeasurementOutcome.Success => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<IReadOnlyList<ClinicalMeasurementResponse>>
                {
                    Status = true,
                    Data = result.Measurements
                }),
            ClinicalMeasurementOutcome.VisitNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            }),
            ClinicalMeasurementOutcome.VisitClosed => Conflict(new BaseResponse
            {
                Status = false,
                Message = "Clinical measurements can only be recorded for an open visit.",
                Error = "visit_closed"
            }),
            ClinicalMeasurementOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "Only the assigned doctor or a Clinic Super User can record measurements."
                }),
            ClinicalMeasurementOutcome.InvalidField => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid clinical field.",
                Error = "invalid_clinical_field"
            }),
            ClinicalMeasurementOutcome.InvalidValue => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid clinical measurement value.",
                Error = "invalid_clinical_measurement"
            }),
            ClinicalMeasurementOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to record clinical measurements."
            })
        };
    }
}
