using Auran.Clinic.Infrastructure.Lookups;

namespace Auran.Clinic.UnitTests;

public sealed class SystemLookupServiceTests
{
    private readonly SystemLookupService _service = new();

    [Fact]
    public void GetTimeZones_ReturnsUniquePopulatedOptions()
    {
        var timeZones = _service.GetTimeZones();

        Assert.NotEmpty(timeZones);
        Assert.Equal(
            timeZones.Count,
            timeZones.Select(timeZone => timeZone.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Assert.All(timeZones, timeZone =>
        {
            Assert.False(string.IsNullOrWhiteSpace(timeZone.Id));
            Assert.False(string.IsNullOrWhiteSpace(timeZone.DisplayName));
            Assert.Matches("^[+-]\\d{2}:\\d{2}$", timeZone.UtcOffset);
            Assert.True(timeZone.DisplayName.Contains(timeZone.Id, StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void GetLocales_ReturnsUniqueNeutralAndSpecificCultures()
    {
        var locales = _service.GetLocales();

        Assert.NotEmpty(locales);
        Assert.Equal(
            locales.Count,
            locales.Select(locale => locale.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(locales, locale => locale.Code.Equals("en", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(locales, locale => locale.Code.Equals("ar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(locales, locale => locale.Code.Equals("en-US", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(locales, locale => locale.Code.Equals("ar-EG", StringComparison.OrdinalIgnoreCase));

        Assert.All(locales, locale =>
        {
            Assert.False(string.IsNullOrWhiteSpace(locale.Code));
            Assert.False(string.IsNullOrWhiteSpace(locale.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(locale.NativeName));
        });
    }
}
