namespace Auran.Clinic.Application.PendingDocumentation;

public sealed class PendingDocumentationQuery
{
    public bool MineOnly { get; init; } = true;
}

public sealed record PendingDocumentationResponse(
    Guid VisitId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string VisitStatus,
    string DocumentationStatus,
    DateTime EntryAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? ExitAtUtc,
    string? ChiefComplaint,
    string? Examination,
    string? Diagnosis,
    string? Notes,
    string? TreatmentPlan);

public sealed class CompletePendingDocumentationRequest
{
    public Guid VisitId { get; init; }
    public string? ChiefComplaint { get; init; }
    public string? Examination { get; init; }
    public string? Diagnosis { get; init; }
    public string? Notes { get; init; }
    public string? TreatmentPlan { get; init; }
}

public enum PendingDocumentationOutcome
{
    Success,
    VisitNotFound,
    Forbidden,
    NotPending,
    Unauthenticated
}

public sealed record PendingDocumentationResult(
    PendingDocumentationOutcome Outcome,
    PendingDocumentationResponse? Documentation = null);
