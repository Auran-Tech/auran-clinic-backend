using Auran.Clinic.Application.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Route("api/reference")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class ReferenceDataController : ControllerBase
{
    [HttpGet("fonts")]
    [SwaggerOperation(Summary = "Get supported fonts", OperationId = "Reference_Fonts", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<ReferenceOptionResponse>> GetFonts() =>
        Ok(ReferenceDataCatalog.Fonts);

    [HttpGet("countries")]
    [SwaggerOperation(Summary = "Get supported countries", OperationId = "Reference_Countries", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<CountryReferenceResponse>> GetCountries() =>
        Ok(ReferenceDataCatalog.Countries);

    [HttpGet("countries/{countryCode}/cities")]
    [SwaggerOperation(Summary = "Get supported cities for a country", OperationId = "Reference_Cities", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<CityReferenceResponse>> GetCities(string countryCode)
    {
        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();
        if (!ReferenceDataCatalog.IsSupportedCountry(normalizedCountryCode))
            return NotFound();

        return Ok(ReferenceDataCatalog.Cities
            .Where(x => x.CountryCode.Equals(normalizedCountryCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name)
            .ToArray());
    }

    [HttpGet("locales")]
    [SwaggerOperation(Summary = "Get supported locales", OperationId = "Reference_Locales", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<ReferenceOptionResponse>> GetLocales() =>
        Ok(ReferenceDataCatalog.Locales);

    [HttpGet("date-formats")]
    [SwaggerOperation(Summary = "Get supported date formats", OperationId = "Reference_DateFormats", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<FormatReferenceResponse>> GetDateFormats() =>
        Ok(ReferenceDataCatalog.DateFormats);

    [HttpGet("time-formats")]
    [SwaggerOperation(Summary = "Get supported time formats", OperationId = "Reference_TimeFormats", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<FormatReferenceResponse>> GetTimeFormats() =>
        Ok(ReferenceDataCatalog.TimeFormats);

    [HttpGet("time-zones")]
    [SwaggerOperation(Summary = "Get supported time zones", Description = "Returns normalized IANA-style time-zone identifiers where the runtime can resolve them, with UTC always available.", OperationId = "Reference_TimeZones", Tags = new[] { "Reference Data" })]
    public ActionResult<IReadOnlyCollection<TimeZoneReferenceResponse>> GetTimeZones() =>
        Ok(ReferenceDataCatalog.GetTimeZones());
}
