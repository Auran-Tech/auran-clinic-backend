namespace Auran.Clinic.Application.Patients;

public sealed class PatientDuplicateCheckRequest
{
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public Guid? ExcludePatientId { get; init; }
}
