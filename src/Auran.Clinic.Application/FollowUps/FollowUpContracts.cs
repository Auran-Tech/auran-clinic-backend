using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.FollowUps;

public sealed record FollowUpResponse(
    Guid Id,
    Guid PatientId,
    Guid VisitId,
    Guid DoctorId,
    string PatientNumber,
    string PatientName,
    string DoctorName,
    string Recommendation,
    int? RecommendedAfterDays,
    DateOnly? RecommendedDate,
    FollowUpStatus Status,
    string DueCategory,
    DateTime CreatedDate,
    DateTime? UpdatedDate);

public sealed class CreateFollowUpRequest
{
    public Guid PatientId { get; init; }
    public Guid VisitId { get; init; }
    public Guid DoctorId { get; init; }
    public required string Recommendation { get; init; }
    public int? RecommendedAfterDays { get; init; }
    public DateOnly? RecommendedDate { get; init; }
}

public sealed class UpdateFollowUpRequest
{
    public Guid FollowUpId { get; init; }
    public required string Recommendation { get; init; }
    public int? RecommendedAfterDays { get; init; }
    public DateOnly? RecommendedDate { get; init; }
}

public sealed class SetFollowUpStatusRequest
{
    public Guid FollowUpId { get; init; }
    public FollowUpStatus Status { get; init; }
}

public enum FollowUpMutationOutcome
{
    Success,
    NotFound,
    ValidationError,
    Unauthenticated
}

public sealed record FollowUpMutationResult(
    FollowUpMutationOutcome Outcome,
    FollowUpResponse? FollowUp = null,
    string? Error = null);

public sealed class CreateFollowUpRequestValidator : AbstractValidator<CreateFollowUpRequest>
{
    public CreateFollowUpRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RecommendedAfterDays).GreaterThanOrEqualTo(0).When(x => x.RecommendedAfterDays.HasValue);
    }
}

public sealed class UpdateFollowUpRequestValidator : AbstractValidator<UpdateFollowUpRequest>
{
    public UpdateFollowUpRequestValidator()
    {
        RuleFor(x => x.FollowUpId).NotEmpty();
        RuleFor(x => x.Recommendation).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.RecommendedAfterDays).GreaterThanOrEqualTo(0).When(x => x.RecommendedAfterDays.HasValue);
    }
}

public sealed class SetFollowUpStatusRequestValidator : AbstractValidator<SetFollowUpStatusRequest>
{
    public SetFollowUpStatusRequestValidator()
    {
        RuleFor(x => x.FollowUpId).NotEmpty();
        RuleFor(x => x.Status).Must(status => status is FollowUpStatus.Completed or FollowUpStatus.Cancelled)
            .WithMessage("Follow-up status can only be completed or cancelled.");
    }
}
