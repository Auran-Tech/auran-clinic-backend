namespace Auran.Clinic.Application.ReferenceData;

public sealed record ReferenceOptionResponse(string Code, string Name);

public sealed record CountryReferenceResponse(string Code, string Name, string PhoneCode);

public sealed record CityReferenceResponse(string Code, string Name, string CountryCode);

public sealed record FormatReferenceResponse(string Code, string Format, string Example);

public sealed record TimeZoneReferenceResponse(string Code, string Name, string Offset);

public static class ReferenceDataCatalog
{
    public static readonly IReadOnlyCollection<ReferenceOptionResponse> Fonts = new[]
    {
        new ReferenceOptionResponse("Inter", "Inter"),
        new ReferenceOptionResponse("Roboto", "Roboto"),
        new ReferenceOptionResponse("Arial", "Arial"),
        new ReferenceOptionResponse("Open Sans", "Open Sans"),
        new ReferenceOptionResponse("Cairo", "Cairo"),
        new ReferenceOptionResponse("Tajawal", "Tajawal")
    };

    public static readonly IReadOnlyCollection<CountryReferenceResponse> Countries = new[]
    {
        new CountryReferenceResponse("EG", "Egypt", "+20"),
        new CountryReferenceResponse("SA", "Saudi Arabia", "+966"),
        new CountryReferenceResponse("AE", "United Arab Emirates", "+971"),
        new CountryReferenceResponse("QA", "Qatar", "+974"),
        new CountryReferenceResponse("KW", "Kuwait", "+965"),
        new CountryReferenceResponse("BH", "Bahrain", "+973"),
        new CountryReferenceResponse("OM", "Oman", "+968"),
        new CountryReferenceResponse("JO", "Jordan", "+962")
    };

    public static readonly IReadOnlyCollection<CityReferenceResponse> Cities = new[]
    {
        new CityReferenceResponse("CAI", "Cairo", "EG"),
        new CityReferenceResponse("GIZ", "Giza", "EG"),
        new CityReferenceResponse("ALX", "Alexandria", "EG"),
        new CityReferenceResponse("MAN", "Mansoura", "EG"),
        new CityReferenceResponse("TAN", "Tanta", "EG"),
        new CityReferenceResponse("ASY", "Asyut", "EG"),
        new CityReferenceResponse("RUH", "Riyadh", "SA"),
        new CityReferenceResponse("JED", "Jeddah", "SA"),
        new CityReferenceResponse("DMM", "Dammam", "SA"),
        new CityReferenceResponse("MED", "Medina", "SA"),
        new CityReferenceResponse("MKK", "Mecca", "SA"),
        new CityReferenceResponse("DXB", "Dubai", "AE"),
        new CityReferenceResponse("AUH", "Abu Dhabi", "AE"),
        new CityReferenceResponse("SHJ", "Sharjah", "AE"),
        new CityReferenceResponse("AJM", "Ajman", "AE"),
        new CityReferenceResponse("DOH", "Doha", "QA"),
        new CityReferenceResponse("KWI", "Kuwait City", "KW"),
        new CityReferenceResponse("MNH", "Manama", "BH"),
        new CityReferenceResponse("MCT", "Muscat", "OM"),
        new CityReferenceResponse("AMM", "Amman", "JO")
    };

    public static readonly IReadOnlyCollection<ReferenceOptionResponse> Locales = new[]
    {
        new ReferenceOptionResponse("en", "English"),
        new ReferenceOptionResponse("en-EG", "English (Egypt)"),
        new ReferenceOptionResponse("ar-EG", "Arabic (Egypt)"),
        new ReferenceOptionResponse("en-SA", "English (Saudi Arabia)"),
        new ReferenceOptionResponse("ar-SA", "Arabic (Saudi Arabia)"),
        new ReferenceOptionResponse("en-AE", "English (United Arab Emirates)"),
        new ReferenceOptionResponse("ar-AE", "Arabic (United Arab Emirates)")
    };

    public static readonly IReadOnlyCollection<FormatReferenceResponse> DateFormats = new[]
    {
        new FormatReferenceResponse("ISO", "yyyy-MM-dd", "2026-08-29"),
        new FormatReferenceResponse("DD_MM_YYYY", "dd/MM/yyyy", "29/08/2026"),
        new FormatReferenceResponse("MM_DD_YYYY", "MM/dd/yyyy", "08/29/2026")
    };

    public static readonly IReadOnlyCollection<FormatReferenceResponse> TimeFormats = new[]
    {
        new FormatReferenceResponse("24_HOUR", "HH:mm", "18:30"),
        new FormatReferenceResponse("12_HOUR", "hh:mm tt", "06:30 PM")
    };

    public static bool IsSupportedFont(string? value) =>
        string.IsNullOrWhiteSpace(value) || Fonts.Any(x => x.Code.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsSupportedCountry(string? value) =>
        string.IsNullOrWhiteSpace(value) || Countries.Any(x => x.Code.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsSupportedCity(string? countryCode, string? cityCode)
    {
        if (string.IsNullOrWhiteSpace(cityCode))
            return true;
        if (string.IsNullOrWhiteSpace(countryCode))
            return false;

        return Cities.Any(x =>
            x.CountryCode.Equals(countryCode.Trim(), StringComparison.OrdinalIgnoreCase)
            && x.Code.Equals(cityCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsSupportedLocale(string? value) =>
        string.IsNullOrWhiteSpace(value) || Locales.Any(x => x.Code.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool IsSupportedDateFormat(string? value) =>
        string.IsNullOrWhiteSpace(value) || DateFormats.Any(x => x.Format.Equals(value.Trim(), StringComparison.Ordinal));

    public static bool IsSupportedTimeFormat(string? value) =>
        string.IsNullOrWhiteSpace(value) || TimeFormats.Any(x => x.Format.Equals(value.Trim(), StringComparison.Ordinal));

    public static bool IsSupportedTimeZone(string? value) =>
        string.IsNullOrWhiteSpace(value) || NormalizeTimeZoneId(value) is not null;

    public static string? NormalizeTimeZoneId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var id = value.Trim();
        if (id.Equals("UTC", StringComparison.OrdinalIgnoreCase)
            || id.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase))
        {
            return "UTC";
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var ianaId)
            && !string.IsNullOrWhiteSpace(ianaId))
        {
            return ianaId;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out _))
            return id;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id);
            return id;
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    public static IReadOnlyCollection<TimeZoneReferenceResponse> GetTimeZones()
    {
        var zones = TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => new TimeZoneReferenceResponse(
                NormalizeTimeZoneId(zone.Id) ?? zone.Id,
                zone.DisplayName,
                FormatOffset(zone.BaseUtcOffset)))
            .Append(new TimeZoneReferenceResponse("UTC", "UTC", "+00:00"))
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(x => x.Offset)
            .ThenBy(x => x.Code)
            .ToArray();

        return zones;
    }

    private static string FormatOffset(TimeSpan offset) =>
        $"{(offset < TimeSpan.Zero ? "-" : "+")}{Math.Abs(offset.Hours):00}:{Math.Abs(offset.Minutes):00}";
}
