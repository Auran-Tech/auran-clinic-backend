namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicDetailsResponse
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public bool IsActive { get; init; }
    public string? TimeZoneId { get; init; }
    public string? PatientNumberPrefix { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Website { get; init; }
    public string? Locale { get; init; }
    public Guid? InitialAdminUserId { get; init; }
    public string? InitialAdminEmail { get; init; }
}
