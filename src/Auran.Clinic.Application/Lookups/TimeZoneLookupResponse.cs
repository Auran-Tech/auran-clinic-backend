namespace Auran.Clinic.Application.Lookups;

public sealed class TimeZoneLookupResponse
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string UtcOffset { get; init; }
}
