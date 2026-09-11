namespace Auran.Clinic.Application.Lookups;

public interface ISystemLookupService
{
    IReadOnlyList<TimeZoneLookupResponse> GetTimeZones();
    IReadOnlyList<LocaleLookupResponse> GetLocales();
}
