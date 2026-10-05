namespace Auran.Clinic.Application.Patients;

public sealed class UpdatePatientRequest
{
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public string? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Notes { get; init; }
}
