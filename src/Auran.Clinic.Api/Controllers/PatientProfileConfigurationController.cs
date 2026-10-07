using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.PatientProfiles;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/patient-profile/configuration/admin")]
[Produces("application/json")]
public sealed class PatientProfileConfigurationController(
    IPatientProfileConfigurationService service,
    IValidator<CreatePatientProfileSectionRequest> createSectionValidator,
    IValidator<UpdatePatientProfileSectionRequest> updateSectionValidator,
    IValidator<CreatePatientProfileFieldRequest> createFieldValidator,
    IValidator<UpdatePatientProfileFieldRequest> updateFieldValidator,
    IValidator<CreatePatientProfileOptionRequest> createOptionValidator,
    IValidator<UpdatePatientProfileOptionRequest> updateOptionValidator,
    IValidator<DeletePatientProfileOptionRequest> deleteOptionValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(
        Summary = "Get patient profile field administration configuration",
        Description = "Returns enabled and disabled sections/fields, select options, and HasValues protection metadata.",
        OperationId = "PatientProfileConfiguration_GetAdmin",
        Tags = new[] { "Settings", "Patients" })]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var data = await service.GetAsync(cancellationToken);
        return Ok(new BaseResponse<PatientProfileAdminConfigurationResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost("sections")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> CreateSection(
        [FromBody] CreatePatientProfileSectionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createSectionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.CreateSectionAsync(request, cancellationToken), created: true);
    }

    [HttpPut("sections")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> UpdateSection(
        [FromBody] UpdatePatientProfileSectionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateSectionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.UpdateSectionAsync(request, cancellationToken));
    }

    [HttpPost("fields")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> CreateField(
        [FromBody] CreatePatientProfileFieldRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createFieldValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.CreateFieldAsync(request, cancellationToken), created: true);
    }

    [HttpPut("fields")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> UpdateField(
        [FromBody] UpdatePatientProfileFieldRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateFieldValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.UpdateFieldAsync(request, cancellationToken));
    }

    [HttpPost("options")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> CreateOption(
        [FromBody] CreatePatientProfileOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.CreateOptionAsync(request, cancellationToken), created: true);
    }

    [HttpPut("options")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> UpdateOption(
        [FromBody] UpdatePatientProfileOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.UpdateOptionAsync(request, cancellationToken));
    }

    [HttpDelete("options")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>>> DeleteOption(
        [FromBody] DeletePatientProfileOptionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await deleteOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationFailure();

        return Map(await service.DeleteOptionAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<PatientProfileAdminConfigurationResponse>> Map(
        PatientProfileConfigurationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            PatientProfileConfigurationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<PatientProfileAdminConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            PatientProfileConfigurationOutcome.Success => Ok(
                new BaseResponse<PatientProfileAdminConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            PatientProfileConfigurationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Patient profile configuration item not found."
            }),
            PatientProfileConfigurationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid patient profile configuration.",
                Error = "profile_configuration_validation_error"
            }),
            PatientProfileConfigurationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Patient profile configuration conflicts with existing patient data.",
                Error = "profile_configuration_conflict"
            }),
            PatientProfileConfigurationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage patient profile configuration."
            })
        };
    }

    private BadRequestObjectResult ValidationFailure() =>
        BadRequest(new BaseResponse<PatientProfileAdminConfigurationResponse>
        {
            Status = false,
            Error = "validation_error"
        });
}
