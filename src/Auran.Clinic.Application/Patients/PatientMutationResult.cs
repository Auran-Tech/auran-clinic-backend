namespace Auran.Clinic.Application.Patients;

public enum PatientMutationOutcome
{
    Success,
    NotFound,
    Conflict,
    ValidationError,
    Unauthenticated
}

public sealed record PatientMutationResult(
    PatientMutationOutcome Outcome,
    PatientResponse? Patient = null,
    string? Error = null);
