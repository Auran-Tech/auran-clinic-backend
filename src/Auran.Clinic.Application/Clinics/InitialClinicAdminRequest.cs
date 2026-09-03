namespace Auran.Clinic.Application.Clinics;

public sealed class InitialClinicAdminRequest
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? Phone { get; init; }
}
