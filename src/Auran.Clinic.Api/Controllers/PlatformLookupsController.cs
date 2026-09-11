using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Lookups;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Platform)]
[Route("api/platform/lookups")]
[Produces("application/json")]
public sealed class PlatformLookupsController(ISystemLookupService lookupService) : ControllerBase
{
    [HttpGet("time-zones")]
    [SwaggerOperation(
        Summary = "List supported time zones",
        Description = "Returns time-zone identifiers suitable for Clinic.TimeZoneId. IANA identifiers are preferred so clients can persist stable cross-platform values.",
        OperationId = "PlatformLookups_ListTimeZones",
        Tags = new[] { "Platform Lookups" })]
    [ProducesResponseType(typeof(BaseResponse<IReadOnlyList<TimeZoneLookupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public ActionResult<BaseResponse<IReadOnlyList<TimeZoneLookupResponse>>> ListTimeZones()
    {
        return Ok(new BaseResponse<IReadOnlyList<TimeZoneLookupResponse>>
        {
            Status = true,
            Data = lookupService.GetTimeZones()
        });
    }

    [HttpGet("locales")]
    [SwaggerOperation(
        Summary = "List supported locales",
        Description = "Returns neutral and specific culture codes suitable for ClinicSettings.Locale, including English and native display names.",
        OperationId = "PlatformLookups_ListLocales",
        Tags = new[] { "Platform Lookups" })]
    [ProducesResponseType(typeof(BaseResponse<IReadOnlyList<LocaleLookupResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status403Forbidden)]
    public ActionResult<BaseResponse<IReadOnlyList<LocaleLookupResponse>>> ListLocales()
    {
        return Ok(new BaseResponse<IReadOnlyList<LocaleLookupResponse>>
        {
            Status = true,
            Data = lookupService.GetLocales()
        });
    }
}
