namespace Auran.Clinic.Application.Clinics;

public sealed class UpdateClinicRequest
{
    public required string Name { get; init; }
    public string? TimeZoneId { get; init; }
    public string? PatientNumberPrefix { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Website { get; init; }
    public string? Locale { get; init; }
}
