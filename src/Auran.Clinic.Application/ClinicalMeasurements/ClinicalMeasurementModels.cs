namespace Auran.Clinic.Application.ClinicalMeasurements;

public sealed record ClinicalMeasurementFieldOptionResponse(
    Guid Id,
    string Label,
    string Value,
    int SortOrder);

public sealed record ClinicalMeasurementFieldResponse(
    Guid Id,
    string Name,
    string FieldType,
    string? Unit,
    int SortOrder,
    IReadOnlyList<ClinicalMeasurementFieldOptionResponse> Options);

public sealed record ClinicalMeasurementResponse(
    Guid Id,
    Guid PatientId,
    Guid? VisitId,
    Guid ClinicalFieldId,
    string FieldName,
    string FieldType,
    string? Unit,
    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue,
    DateOnly? DateValue,
    string? JsonValue,
    DateTime RecordedAtUtc,
    Guid RecordedByUserId);

public sealed class RecordClinicalMeasurementValueRequest
{
    public Guid FieldId { get; init; }
    public string? TextValue { get; init; }
    public decimal? NumberValue { get; init; }
    public bool? BooleanValue { get; init; }
    public DateOnly? DateValue { get; init; }
    public string? JsonValue { get; init; }
}

public sealed class RecordClinicalMeasurementsRequest
{
    public Guid VisitId { get; init; }
    public IReadOnlyCollection<RecordClinicalMeasurementValueRequest> Values { get; init; } =
        Array.Empty<RecordClinicalMeasurementValueRequest>();
}

public enum ClinicalMeasurementOutcome
{
    Success,
    VisitNotFound,
    VisitClosed,
    Forbidden,
    InvalidField,
    InvalidValue,
    Unauthenticated
}

public sealed record ClinicalMeasurementResult(
    ClinicalMeasurementOutcome Outcome,
    IReadOnlyList<ClinicalMeasurementResponse>? Measurements = null,
    string? Error = null);
