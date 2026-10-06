namespace Auran.Clinic.Application.ClinicalOrders;

public sealed record ClinicalOrderSectionDefinitionResponse(
    Guid Id,
    string Name,
    string SectionType,
    int SortOrder);

public sealed record ClinicalOrderItemResponse(
    Guid Id,
    string Name,
    string? DetailsJson);

public sealed record ClinicalOrderSectionResponse(
    Guid Id,
    Guid SectionDefinitionId,
    string SectionName,
    string SectionType,
    int SortOrder,
    string? TextValue,
    IReadOnlyList<ClinicalOrderItemResponse> Items);

public sealed record ClinicalOrderResponse(
    Guid Id,
    Guid VisitId,
    Guid PatientId,
    Guid DoctorId,
    IReadOnlyList<ClinicalOrderSectionResponse> Sections);

public sealed class SaveClinicalOrderItemRequest
{
    public string Name { get; init; } = string.Empty;
    public string? DetailsJson { get; init; }
}

public sealed class SaveClinicalOrderSectionRequest
{
    public Guid SectionDefinitionId { get; init; }
    public string? TextValue { get; init; }
    public IReadOnlyCollection<SaveClinicalOrderItemRequest> Items { get; init; } = Array.Empty<SaveClinicalOrderItemRequest>();
}

public sealed class SaveClinicalOrderRequest
{
    public Guid VisitId { get; init; }
    public IReadOnlyCollection<SaveClinicalOrderSectionRequest> Sections { get; init; } = Array.Empty<SaveClinicalOrderSectionRequest>();
}

public enum ClinicalOrderOutcome
{
    Success,
    VisitNotFound,
    Forbidden,
    InvalidSectionDefinition,
    Unauthenticated
}

public sealed record ClinicalOrderResult(
    ClinicalOrderOutcome Outcome,
    ClinicalOrderResponse? Order = null,
    string? Error = null);
