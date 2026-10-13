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
[Route("api/clinical-measurements/configuration/admin")]
[Produces("application/json")]
public sealed class ClinicalFieldConfigurationController(
    IClinicalFieldConfigurationService service,
    IValidator<CreateClinicalFieldRequest> createFieldValidator,
    IValidator<UpdateClinicalFieldRequest> updateFieldValidator,
    IValidator<CreateClinicalFieldOptionRequest> createOptionValidator,
    IValidator<UpdateClinicalFieldOptionRequest> updateOptionValidator,
    IValidator<DeleteClinicalFieldOptionRequest> deleteOptionValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(
        Summary = "Get clinical field administration configuration",
        Description = "Returns enabled/disabled clinical fields, measurement-history protection metadata, and select options.",
        OperationId = "ClinicalFieldConfiguration_GetAdmin",
        Tags = new[] { "Settings", "Clinical Measurements" })]
    public async Task<ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var data = await service.GetAsync(cancellationToken);
        return Ok(new BaseResponse<ClinicalFieldAdminConfigurationResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost("fields")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>>> CreateField(
        [FromBody] CreateClinicalFieldRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createFieldValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.CreateFieldAsync(request, cancellationToken), created: true);
    }

    [HttpPut("fields")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>>> UpdateField(
        [FromBody] UpdateClinicalFieldRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateFieldValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.UpdateFieldAsync(request, cancellationToken));
    }

    [HttpPost("options")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>>> CreateOption(
        [FromBody] CreateClinicalFieldOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.CreateOptionAsync(request, cancellationToken), created: true);
    }

    [HttpPut("options")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>>> UpdateOption(
        [FromBody] UpdateClinicalFieldOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.UpdateOptionAsync(request, cancellationToken));
    }

    [HttpDelete("options")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>>> DeleteOption(
        [FromBody] DeleteClinicalFieldOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.DeleteOptionAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<ClinicalFieldAdminConfigurationResponse>> Map(
        ClinicalFieldConfigurationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            ClinicalFieldConfigurationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<ClinicalFieldAdminConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            ClinicalFieldConfigurationOutcome.Success => Ok(
                new BaseResponse<ClinicalFieldAdminConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            ClinicalFieldConfigurationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Clinical field configuration item not found."
            }),
            ClinicalFieldConfigurationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid clinical field configuration.",
                Error = "clinical_field_configuration_validation_error"
            }),
            ClinicalFieldConfigurationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Clinical field configuration conflicts with measurement history.",
                Error = "clinical_field_configuration_conflict"
            }),
            ClinicalFieldConfigurationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage clinical field configuration."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure() =>
        BadRequest(new BaseResponse<ClinicalFieldAdminConfigurationResponse>
        {
            Status = false,
            Error = "validation_error"
        });
}
