using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/clinical-orders")]
[Produces("application/json")]
public sealed class ClinicalOrdersController(
    IClinicalOrderService clinicalOrderService,
    IValidator<SaveClinicalOrderRequest> saveOrderValidator,
    IValidator<SaveClinicalOrderSectionDefinitionsRequest> definitionsValidator) : ControllerBase
{
    [HttpGet("definitions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>>>> Definitions(
        CancellationToken cancellationToken)
    {
        var result = await clinicalOrderService.GetDefinitionsAsync(cancellationToken);
        return Ok(new BaseResponse<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>>
        {
            Status = true,
            Data = result
        });
    }

    [HttpPost("details")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.View)]
    public async Task<ActionResult<BaseResponse<ClinicalOrderResponse>>> Details(
        [FromBody] ClinicalOrderLookupRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VisitId == Guid.Empty)
            return BadRequest(new BaseResponse<ClinicalOrderResponse> { Status = false, Error = "validation_error" });

        var result = await clinicalOrderService.GetByVisitAsync(request.VisitId, cancellationToken);
        return Ok(new BaseResponse<ClinicalOrderResponse>
        {
            Status = true,
            Data = result
        });
    }

    [HttpPut]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Visits.Edit)]
    public async Task<ActionResult<BaseResponse<ClinicalOrderResponse>>> Save(
        [FromBody] SaveClinicalOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await saveOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<ClinicalOrderResponse> { Status = false, Error = "validation_error" });

        var result = await clinicalOrderService.SaveAsync(request, cancellationToken);
        if (result is null)
            return NotFound(new BaseResponse { Status = false, Message = "Visit or clinical order section definition not found." });

        return Ok(new BaseResponse<ClinicalOrderResponse> { Status = true, Data = result });
    }

    [HttpPut("definitions")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Settings.Manage)]
    public async Task<ActionResult<BaseResponse<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>>>> SaveDefinitions(
        [FromBody] SaveClinicalOrderSectionDefinitionsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await definitionsValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>>
            {
                Status = false,
                Error = "validation_error"
            });

        var result = await clinicalOrderService.SaveDefinitionsAsync(request, cancellationToken);
        if (result.Definitions is null)
        {
            return Conflict(new BaseResponse<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>>
            {
                Status = false,
                Message = result.Error ?? "Clinical order definitions could not be saved.",
                Error = "clinical_order_definition_conflict"
            });
        }

        return Ok(new BaseResponse<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>>
        {
            Status = true,
            Data = result.Definitions
        });
    }
}
