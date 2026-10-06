namespace Auran.Clinic.Application.Patients;

public sealed class PatientQuery
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class CreatePatientRequest
{
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public string? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdatePatientRequest
{
    public Guid PatientId { get; init; }
    public required string FullName { get; init; }
    public required string Phone { get; init; }
    public string? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Notes { get; init; }
}

public sealed record PatientResponse(
    Guid Id,
    string PatientNumber,
    string FullName,
    string Phone,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Notes,
    DateTime CreatedDate,
    DateTime? UpdatedDate);

public sealed record PatientDuplicateCandidateResponse(
    Guid Id,
    string PatientNumber,
    string FullName,
    string Phone,
    DateOnly? DateOfBirth,
    string MatchReason);

public enum PatientManagementOutcome
{
    Success,
    NotFound,
    Conflict,
    ValidationError,
    Unauthenticated
}

public sealed record PatientManagementResult(
    PatientManagementOutcome Outcome,
    PatientResponse? Patient = null,
    string? Error = null);
