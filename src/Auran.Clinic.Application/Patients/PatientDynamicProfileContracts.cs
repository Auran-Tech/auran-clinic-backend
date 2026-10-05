using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.Patients;

public sealed record DynamicFieldOptionResponse(Guid Id, string Label, string Value, int SortOrder);

public sealed record PatientDynamicFieldResponse(
    Guid FieldId,
    string Label,
    DynamicFieldType FieldType,
    bool IsRequired,
    int SortOrder,
    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue,
    DateOnly? DateValue,
    string? JsonValue,
    IReadOnlyCollection<DynamicFieldOptionResponse> Options);

public sealed record PatientDynamicSectionResponse(
    Guid SectionId,
    string Name,
    int SortOrder,
    IReadOnlyCollection<PatientDynamicFieldResponse> Fields);

public sealed record PatientDynamicProfileResponse(
    Guid PatientId,
    IReadOnlyCollection<PatientDynamicSectionResponse> Sections);

public sealed class SavePatientDynamicValueRequest
{
    public Guid PatientId { get; init; }
    public Guid FieldId { get; init; }
    public string? TextValue { get; init; }
    public decimal? NumberValue { get; init; }
    public bool? BooleanValue { get; init; }
    public DateOnly? DateValue { get; init; }
    public string? JsonValue { get; init; }
}

public sealed class SavePatientDynamicValueRequestValidator : AbstractValidator<SavePatientDynamicValueRequest>
{
    public SavePatientDynamicValueRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.FieldId).NotEmpty();
        RuleFor(x => x.TextValue).MaximumLength(4000);
        RuleFor(x => x.JsonValue).MaximumLength(12000);
    }
}
