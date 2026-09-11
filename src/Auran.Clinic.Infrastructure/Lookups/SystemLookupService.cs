using System.Globalization;
using Auran.Clinic.Application.Lookups;

namespace Auran.Clinic.Infrastructure.Lookups;

public sealed class SystemLookupService : ISystemLookupService
{
    private static readonly IReadOnlyList<TimeZoneLookupResponse> TimeZones = BuildTimeZones();
    private static readonly IReadOnlyList<LocaleLookupResponse> Locales = BuildLocales();

    public IReadOnlyList<TimeZoneLookupResponse> GetTimeZones() => TimeZones;

    public IReadOnlyList<LocaleLookupResponse> GetLocales() => Locales;

    private static IReadOnlyList<TimeZoneLookupResponse> BuildTimeZones()
    {
        return TimeZoneInfo.GetSystemTimeZones()
            .Select(timeZone => new
            {
                Id = NormalizeTimeZoneId(timeZone.Id),
                timeZone.BaseUtcOffset
            })
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.BaseUtcOffset)
            .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(item => new TimeZoneLookupResponse
            {
                Id = item.Id,
                UtcOffset = FormatUtcOffset(item.BaseUtcOffset),
                DisplayName = $"(UTC{FormatUtcOffset(item.BaseUtcOffset)}) {item.Id}"
            })
            .ToArray();
    }

    private static IReadOnlyList<LocaleLookupResponse> BuildLocales()
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Where(culture => !string.IsNullOrWhiteSpace(culture.Name))
            .GroupBy(culture => culture.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(culture => culture.EnglishName, StringComparer.OrdinalIgnoreCase)
            .Select(culture => new LocaleLookupResponse
            {
                Code = culture.Name,
                DisplayName = culture.EnglishName,
                NativeName = culture.NativeName
            })
            .ToArray();
    }

    private static string NormalizeTimeZoneId(string timeZoneId)
    {
        return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out var ianaId)
            && !string.IsNullOrWhiteSpace(ianaId)
                ? ianaId
                : timeZoneId;
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absoluteOffset = offset.Duration();
        return $"{sign}{absoluteOffset.Hours:00}:{absoluteOffset.Minutes:00}";
    }
}
