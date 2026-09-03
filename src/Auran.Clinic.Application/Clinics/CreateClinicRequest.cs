namespace Auran.Clinic.Application.Clinics;

public sealed class CreateClinicRequest
{
    public required string Name { get; init; }
    public required string CodePrefix { get; init; }
    public string? TimeZoneId { get; init; }
    public string? PatientNumberPrefix { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Website { get; init; }
    public string? Locale { get; init; }
    public required InitialClinicAdminRequest Admin { get; init; }
}
