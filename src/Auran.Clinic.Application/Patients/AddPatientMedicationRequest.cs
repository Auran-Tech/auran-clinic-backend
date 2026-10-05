namespace Auran.Clinic.Application.Patients;

public sealed class AddPatientMedicationRequest
{
    public Guid PatientId { get; init; }
    public required string Name { get; init; }
    public string? Dosage { get; init; }
    public string? Notes { get; init; }
}
