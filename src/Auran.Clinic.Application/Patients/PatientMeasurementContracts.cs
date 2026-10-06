using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.Patients;

public sealed record ClinicalFieldOptionResponse(Guid Id, string Label, string Value, int SortOrder);

public sealed record ClinicalFieldResponse(
    Guid Id,
    string Name,
    DynamicFieldType FieldType,
    string? Unit,
    int SortOrder,
    IReadOnlyCollection<ClinicalFieldOptionResponse> Options);

public sealed record ClinicalMeasurementResponse(
    Guid Id,
    Guid ClinicalFieldId,
    string FieldName,
    string? Unit,
    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue,
    DateOnly? DateValue,
    string? JsonValue,
    DateTime RecordedAtUtc);

public sealed record PatientMeasurementsResponse(
    Guid PatientId,
    IReadOnlyCollection<ClinicalFieldResponse> Fields,
    IReadOnlyCollection<ClinicalMeasurementResponse> Measurements);

public sealed class AddClinicalMeasurementRequest
{
    public Guid PatientId { get; init; }
    public Guid ClinicalFieldId { get; init; }
    public string? TextValue { get; init; }
    public decimal? NumberValue { get; init; }
    public bool? BooleanValue { get; init; }
    public DateOnly? DateValue { get; init; }
    public string? JsonValue { get; init; }
}

public sealed class AddClinicalMeasurementRequestValidator : AbstractValidator<AddClinicalMeasurementRequest>
{
    public AddClinicalMeasurementRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ClinicalFieldId).NotEmpty();
        RuleFor(x => x.TextValue).MaximumLength(4000);
        RuleFor(x => x.JsonValue).MaximumLength(12000);
    }
}
