namespace Auran.Clinic.Application.Patients;

public sealed record PatientAllergyResponse(
    Guid Id,
    string Name,
    string? Reaction,
    string? Notes,
    DateTime RecordedAtUtc);

public sealed record PatientConditionResponse(
    Guid Id,
    string Name,
    string? Notes,
    DateTime RecordedAtUtc);

public sealed record PatientMedicationResponse(
    Guid Id,
    string Name,
    string? Dosage,
    string? Notes,
    DateTime RecordedAtUtc);

public sealed record PatientClinicalProfileResponse(
    PatientResponse Patient,
    IReadOnlyCollection<PatientAllergyResponse> Allergies,
    IReadOnlyCollection<PatientConditionResponse> Conditions,
    IReadOnlyCollection<PatientMedicationResponse> Medications);
