namespace Auran.Clinic.Application.ClinicalMeasurements;

public sealed record ClinicalFieldAdminOptionResponse(
    Guid Id,
    string Label,
    string Value,
    int SortOrder);

public sealed record ClinicalFieldAdminResponse(
    Guid Id,
    string Name,
    string FieldType,
    string? Unit,
    bool IsEnabled,
    int SortOrder,
    bool HasMeasurements,
    IReadOnlyList<ClinicalFieldAdminOptionResponse> Options);

public sealed record ClinicalFieldAdminConfigurationResponse(
    IReadOnlyList<ClinicalFieldAdminResponse> Fields);

public sealed class CreateClinicalFieldRequest
{
    public string Name { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public int SortOrder { get; init; }
}

public sealed class UpdateClinicalFieldRequest
{
    public Guid FieldId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public string? Unit { get; init; }
    public bool IsEnabled { get; init; }
    public int SortOrder { get; init; }
}

public sealed class CreateClinicalFieldOptionRequest
{
    public Guid FieldId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class UpdateClinicalFieldOptionRequest
{
    public Guid OptionId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class DeleteClinicalFieldOptionRequest
{
    public Guid OptionId { get; init; }
}

public enum ClinicalFieldConfigurationOutcome
{
    Success,
    NotFound,
    ValidationError,
    Conflict,
    Unauthenticated
}

public sealed record ClinicalFieldConfigurationResult(
    ClinicalFieldConfigurationOutcome Outcome,
    ClinicalFieldAdminConfigurationResponse? Configuration = null,
    string? Error = null);
