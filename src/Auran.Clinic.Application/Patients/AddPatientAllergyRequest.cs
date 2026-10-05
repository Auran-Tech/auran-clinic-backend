namespace Auran.Clinic.Application.Patients;

public sealed class AddPatientAllergyRequest
{
    public Guid PatientId { get; init; }
    public required string Name { get; init; }
    public string? Reaction { get; init; }
    public string? Notes { get; init; }
}
