using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.Visits;

public sealed record VisitSessionResponse(
    Guid Id,
    Guid DoctorId,
    string DoctorName,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc);

public sealed record VisitSummaryResponse(
    Guid Id,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    VisitStatus Status,
    DocumentationStatus DocumentationStatus,
    DateTime EntryAtUtc,
    DateTime? CompletedAtUtc,
    string RowVersion);

public sealed record VisitDoctorResponse(
    Guid Id,
    string FullName);

public sealed record VisitDetailsResponse(
    VisitSummaryResponse Visit,
    string? ChiefComplaint,
    string? Examination,
    string? Diagnosis,
    string? Notes,
    string? TreatmentPlan,
    IReadOnlyCollection<VisitSessionResponse> Sessions,
    IReadOnlyCollection<VisitDoctorResponse> AvailableDoctors);

public sealed class VisitLookupRequest
{
    public Guid VisitId { get; init; }
}

public sealed class StartVisitSessionRequest
{
    public Guid VisitId { get; init; }
    public Guid DoctorId { get; init; }
}

public sealed class EndVisitSessionRequest
{
    public Guid VisitId { get; init; }
    public Guid SessionId { get; init; }
}

public sealed class SaveVisitDraftRequest
{
    public Guid VisitId { get; init; }
    public required string RowVersion { get; init; }
    public string? ChiefComplaint { get; init; }
    public string? Examination { get; init; }
    public string? Diagnosis { get; init; }
    public string? Notes { get; init; }
    public string? TreatmentPlan { get; init; }
}

public enum VisitMutationOutcome
{
    Success,
    NotFound,
    Conflict,
    ValidationError,
    Unauthenticated
}

public sealed record VisitMutationResult(
    VisitMutationOutcome Outcome,
    VisitDetailsResponse? Visit = null,
    string? Error = null);

public sealed class VisitLookupRequestValidator : AbstractValidator<VisitLookupRequest>
{
    public VisitLookupRequestValidator() => RuleFor(x => x.VisitId).NotEmpty();
}

public sealed class StartVisitSessionRequestValidator : AbstractValidator<StartVisitSessionRequest>
{
    public StartVisitSessionRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
    }
}

public sealed class EndVisitSessionRequestValidator : AbstractValidator<EndVisitSessionRequest>
{
    public EndVisitSessionRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public sealed class SaveVisitDraftRequestValidator : AbstractValidator<SaveVisitDraftRequest>
{
    public SaveVisitDraftRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.ChiefComplaint).MaximumLength(8000);
        RuleFor(x => x.Examination).MaximumLength(12000);
        RuleFor(x => x.Diagnosis).MaximumLength(8000);
        RuleFor(x => x.Notes).MaximumLength(12000);
        RuleFor(x => x.TreatmentPlan).MaximumLength(12000);
    }
}
