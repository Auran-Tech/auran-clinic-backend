using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/platform/clinics")]
[Authorize(Policy = ActorPolicies.Platform)]
[Produces("application/json")]
public sealed class PlatformClinicsController(IPlatformClinicProvisioningService provisioningService) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Provision a clinic",
        Description = "Atomically creates a clinic, baseline settings, and its first active Clinic SuperUser. Only authenticated Platform actors can provision clinics.",
        OperationId = "PlatformClinics_Provision",
        Tags = new[] { "Platform Clinics" })]
    [ProducesResponseType(typeof(BaseResponse<ClinicProvisioningResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BaseResponse<ClinicProvisioningResult>>> Provision(
        [FromBody] CreateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var result = await provisioningService.ProvisionAsync(request, cancellationToken);
        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                new BaseResponse<ClinicProvisioningResult> { Status = true, Data = result });
        }

        var response = new BaseResponse
        {
            Status = false,
            Message = result.Error ?? "Clinic provisioning failed."
        };

        if (result.Failure == ClinicProvisioningFailure.Conflict)
            return Conflict(response);

        return BadRequest(response);
    }
}
