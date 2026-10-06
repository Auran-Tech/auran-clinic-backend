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
[Route("api/clinical-orders")]
[Produces("application/json")]
public sealed class ClinicalOrdersController(
    IClinicalOrderService clinicalOrderService,
    IValidator<SaveClinicalOrderRequest> saveValidator) : ControllerBase
{
    [HttpGet("definitions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "List enabled clinical order sections",
        Description = "Returns enabled Text and Structured clinical-order section definitions for the authenticated clinic.",
        OperationId = "ClinicalOrders_ListDefinitions",
        Tags = new[] { "Clinical Orders" })]
    public async Task<ActionResult<BaseResponse<IReadOnlyList<ClinicalOrderSectionDefinitionResponse>>>> Definitions(
        CancellationToken cancellationToken)
    {
        var data = await clinicalOrderService.ListDefinitionsAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<ClinicalOrderSectionDefinitionResponse>>
        {
            Status = true,
            Data = data
        });
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    [SwaggerOperation(
        Summary = "Get the clinical order for a visit",
        Description = "Returns the visit's clinical order with its configured sections and structured items.",
        OperationId = "ClinicalOrders_GetForVisit",
        Tags = new[] { "Clinical Orders" })]
    public async Task<ActionResult<BaseResponse<ClinicalOrderResponse>>> Get(
        [FromQuery] Guid visitId,
        CancellationToken cancellationToken)
    {
        if (visitId == Guid.Empty)
        {
            return BadRequest(new BaseResponse<ClinicalOrderResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var order = await clinicalOrderService.GetForVisitAsync(visitId, cancellationToken);
        if (order is null)
        {
            return NotFound(new BaseResponse
            {
                Status = false,
                Message = "Clinical order not found."
            });
        }

        return Ok(new BaseResponse<ClinicalOrderResponse>
        {
            Status = true,
            Data = order
        });
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    [SwaggerOperation(
        Summary = "Save a clinical order",
        Description = "Creates or replaces the visit's clinical-order sections using enabled clinic definitions. Assigned doctor or Clinic Super User only.",
        OperationId = "ClinicalOrders_Save",
        Tags = new[] { "Clinical Orders" })]
    public async Task<ActionResult<BaseResponse<ClinicalOrderResponse>>> Save(
        [FromBody] SaveClinicalOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await saveValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<ClinicalOrderResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var result = await clinicalOrderService.SaveAsync(request, cancellationToken);
        return result.Outcome switch
        {
            ClinicalOrderOutcome.Success => Ok(new BaseResponse<ClinicalOrderResponse>
            {
                Status = true,
                Data = result.Order
            }),
            ClinicalOrderOutcome.VisitNotFound => NotFound(new BaseResponse
            {
                Status = false,
                Message = "Visit not found."
            }),
            ClinicalOrderOutcome.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new BaseResponse
                {
                    Status = false,
                    Message = "Only the assigned doctor or a Clinic Super User can manage this clinical order."
                }),
            ClinicalOrderOutcome.InvalidSectionDefinition => BadRequest(new BaseResponse
            {
                Status = false,
                Message = result.Error,
                Error = "invalid_section_definition"
            }),
            ClinicalOrderOutcome.Unauthenticated => Unauthorized(new BaseResponse
            {
                Status = false,
                Message = "Authentication is required."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse
            {
                Status = false,
                Message = "Unable to save the clinical order."
            })
        };
    }
}
