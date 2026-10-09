using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/clinical-orders/configuration/admin")]
[Produces("application/json")]
public sealed class ClinicalOrderSectionConfigurationController(
    IClinicalOrderSectionConfigurationService service,
    IValidator<CreateClinicalOrderSectionDefinitionRequest> createValidator,
    IValidator<UpdateClinicalOrderSectionDefinitionRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.View)]
    [SwaggerOperation(
        Summary = "Get clinical order section configuration",
        Description = "Returns enabled and disabled section definitions plus HasData protection metadata.",
        OperationId = "ClinicalOrderSectionConfiguration_Get",
        Tags = new[] { "Settings", "Clinical Orders" })]
    public async Task<ActionResult<BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var data = await service.GetAsync(cancellationToken);
        return Ok(new BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>>> Create(
        [FromBody] CreateClinicalOrderSectionDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        return Map(await service.CreateAsync(request, cancellationToken), created: true);
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>>> Update(
        [FromBody] UpdateClinicalOrderSectionDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        return Map(await service.UpdateAsync(request, cancellationToken));
    }

    private ActionResult<BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>> Map(
        ClinicalOrderSectionConfigurationResult result,
        bool created = false)
    {
        return result.Outcome switch
        {
            ClinicalOrderSectionConfigurationOutcome.Success when created => StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            ClinicalOrderSectionConfigurationOutcome.Success => Ok(
                new BaseResponse<ClinicalOrderSectionAdminConfigurationResponse>
                {
                    Status = true,
                    Data = result.Configuration
                }),
            ClinicalOrderSectionConfigurationOutcome.NotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Clinical order section definition not found."
            }),
            ClinicalOrderSectionConfigurationOutcome.ValidationError => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Invalid clinical order section configuration.",
                Error = "clinical_order_section_validation_error"
            }),
            ClinicalOrderSectionConfigurationOutcome.Conflict => Conflict(new BaseResponse
            {
                Status = false,
                Message = result.Error ?? "Clinical order section configuration conflicts with existing data.",
                Error = "clinical_order_section_conflict"
            }),
            ClinicalOrderSectionConfigurationOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to manage clinical order section configuration."
            })
        };
    }
}
