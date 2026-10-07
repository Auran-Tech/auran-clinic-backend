namespace Auran.Clinic.Application.PatientProfiles;

public sealed record PatientProfileFieldOptionResponse(
    Guid Id,
    string Label,
    string Value,
    int SortOrder);

public sealed record PatientProfileFieldResponse(
    Guid Id,
    string Label,
    string FieldType,
    bool IsRequired,
    int SortOrder,
    IReadOnlyList<PatientProfileFieldOptionResponse> Options);

public sealed record PatientProfileSectionResponse(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsSystem,
    IReadOnlyList<PatientProfileFieldResponse> Fields);

public sealed record PatientProfileConfigurationResponse(
    IReadOnlyList<PatientProfileSectionResponse> Sections);

public sealed record PatientProfileValueResponse(
    Guid FieldId,
    string? TextValue,
    decimal? NumberValue,
    bool? BooleanValue,
    DateOnly? DateValue,
    Guid? FileId,
    string? JsonValue);

public sealed record PatientProfileResponse(
    Guid PatientId,
    IReadOnlyList<PatientProfileValueResponse> Values);

public sealed class SavePatientProfileValueRequest
{
    public Guid FieldId { get; init; }
    public string? TextValue { get; init; }
    public decimal? NumberValue { get; init; }
    public bool? BooleanValue { get; init; }
    public DateOnly? DateValue { get; init; }
    public string? JsonValue { get; init; }
}

public sealed class SavePatientProfileRequest
{
    public Guid PatientId { get; init; }
    public IReadOnlyCollection<SavePatientProfileValueRequest> Values { get; init; } =
        Array.Empty<SavePatientProfileValueRequest>();
}

public enum PatientProfileOutcome
{
    Success,
    PatientNotFound,
    InvalidField,
    InvalidValue,
    RequiredValueMissing,
    Unauthenticated
}

public sealed record PatientProfileResult(
    PatientProfileOutcome Outcome,
    PatientProfileResponse? Profile = null,
    string? Error = null);
