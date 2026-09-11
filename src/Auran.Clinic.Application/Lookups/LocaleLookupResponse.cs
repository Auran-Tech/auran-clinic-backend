namespace Auran.Clinic.Application.Lookups;

public sealed class LocaleLookupResponse
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
    public required string NativeName { get; init; }
}
