namespace Auran.Clinic.Application.PatientMedicalHistory;

public sealed record PatientConditionResponse(
    Guid Id,
    string Name,
    string? Notes,
    DateTime RecordedAtUtc,
    Guid RecordedByUserId);

public sealed record PatientAllergyResponse(
    Guid Id,
    string Name,
    string? Reaction,
    string? Notes,
    DateTime RecordedAtUtc,
    Guid RecordedByUserId);

public sealed record PatientMedicationResponse(
    Guid Id,
    string Name,
    string? Dosage,
    string? Notes,
    DateTime RecordedAtUtc,
    Guid RecordedByUserId);

public sealed record PatientMedicalHistoryResponse(
    Guid PatientId,
    IReadOnlyList<PatientConditionResponse> Conditions,
    IReadOnlyList<PatientAllergyResponse> Allergies,
    IReadOnlyList<PatientMedicationResponse> Medications);

public sealed class CreatePatientConditionRequest
{
    public Guid PatientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public sealed class CreatePatientAllergyRequest
{
    public Guid PatientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Reaction { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreatePatientMedicationRequest
{
    public Guid PatientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Dosage { get; init; }
    public string? Notes { get; init; }
}

public sealed class DeletePatientMedicalHistoryItemRequest
{
    public Guid ItemId { get; init; }
}

public enum PatientMedicalHistoryOutcome
{
    Success,
    PatientNotFound,
    ItemNotFound,
    Conflict,
    Unauthenticated
}

public sealed record PatientMedicalHistoryResult(
    PatientMedicalHistoryOutcome Outcome,
    PatientMedicalHistoryResponse? History = null,
    string? Error = null);
