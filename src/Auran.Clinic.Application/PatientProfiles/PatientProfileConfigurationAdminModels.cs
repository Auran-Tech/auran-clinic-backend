namespace Auran.Clinic.Application.PatientProfiles;

public sealed record PatientProfileAdminOptionResponse(
    Guid Id,
    string Label,
    string Value,
    int SortOrder);

public sealed record PatientProfileAdminFieldResponse(
    Guid Id,
    Guid SectionId,
    string Label,
    string FieldType,
    bool IsRequired,
    bool IsEnabled,
    int SortOrder,
    bool HasValues,
    IReadOnlyList<PatientProfileAdminOptionResponse> Options);

public sealed record PatientProfileAdminSectionResponse(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsSystem,
    bool IsEnabled,
    IReadOnlyList<PatientProfileAdminFieldResponse> Fields);

public sealed record PatientProfileAdminConfigurationResponse(
    IReadOnlyList<PatientProfileAdminSectionResponse> Sections);

public sealed class CreatePatientProfileSectionRequest
{
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class UpdatePatientProfileSectionRequest
{
    public Guid SectionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class CreatePatientProfileFieldRequest
{
    public Guid SectionId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
}

public sealed class UpdatePatientProfileFieldRequest
{
    public Guid FieldId { get; init; }
    public Guid SectionId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsEnabled { get; init; }
    public int SortOrder { get; init; }
}

public sealed class CreatePatientProfileOptionRequest
{
    public Guid FieldId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class UpdatePatientProfileOptionRequest
{
    public Guid OptionId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class DeletePatientProfileOptionRequest
{
    public Guid OptionId { get; init; }
}

public enum PatientProfileConfigurationOutcome
{
    Success,
    NotFound,
    ValidationError,
    Conflict,
    Unauthenticated
}

public sealed record PatientProfileConfigurationResult(
    PatientProfileConfigurationOutcome Outcome,
    PatientProfileAdminConfigurationResponse? Configuration = null,
    string? Error = null);
