namespace Auran.Clinic.Application.ClinicalSessions;

public sealed class StartClinicalSessionRequest
{
    public Guid VisitId { get; init; }
}

public sealed class SaveClinicalDocumentationRequest
{
    public Guid VisitId { get; init; }
    public string? ChiefComplaint { get; init; }
    public string? Examination { get; init; }
    public string? Diagnosis { get; init; }
    public string? Notes { get; init; }
    public string? TreatmentPlan { get; init; }
}

public sealed class EndClinicalSessionRequest
{
    public Guid VisitId { get; init; }
}

public sealed record ClinicalSessionResponse(
    Guid SessionId,
    Guid VisitId,
    Guid DoctorId,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string DocumentationStatus,
    string? ChiefComplaint,
    string? Examination,
    string? Diagnosis,
    string? Notes,
    string? TreatmentPlan);

public enum ClinicalSessionOutcome
{
    Success,
    VisitNotFound,
    Forbidden,
    SessionAlreadyActive,
    SessionNotFound,
    VisitClosed,
    Unauthenticated
}

public sealed record ClinicalSessionResult(
    ClinicalSessionOutcome Outcome,
    ClinicalSessionResponse? Session = null,
    string? Error = null);
