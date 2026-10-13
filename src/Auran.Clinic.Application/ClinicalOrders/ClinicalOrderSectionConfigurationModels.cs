namespace Auran.Clinic.Application.ClinicalOrders;

public sealed record ClinicalOrderSectionAdminResponse(
    Guid Id,
    string Name,
    string SectionType,
    int SortOrder,
    bool IsEnabled,
    bool HasData);

public sealed record ClinicalOrderSectionAdminConfigurationResponse(
    IReadOnlyList<ClinicalOrderSectionAdminResponse> Sections);

public sealed class CreateClinicalOrderSectionDefinitionRequest
{
    public string Name { get; init; } = string.Empty;
    public string SectionType { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class UpdateClinicalOrderSectionDefinitionRequest
{
    public Guid SectionDefinitionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SectionType { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsEnabled { get; init; }
}

public enum ClinicalOrderSectionConfigurationOutcome
{
    Success,
    NotFound,
    ValidationError,
    Conflict,
    Unauthenticated
}

public sealed record ClinicalOrderSectionConfigurationResult(
    ClinicalOrderSectionConfigurationOutcome Outcome,
    ClinicalOrderSectionAdminConfigurationResponse? Configuration = null,
    string? Error = null);
