using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.ClinicalOrders;

public sealed record ClinicalOrderSectionDefinitionResponse(
    string Code,
    string Name,
    ClinicalOrderSectionType SectionType,
    int SortOrder,
    bool IsEnabled);

public sealed record ClinicalOrderItemResponse(
    Guid Id,
    string Name,
    string? DetailsJson);

public sealed record ClinicalOrderSectionResponse(
    Guid Id,
    string DefinitionCode,
    string Name,
    ClinicalOrderSectionType SectionType,
    int SortOrder,
    string? TextValue,
    IReadOnlyCollection<ClinicalOrderItemResponse> Items);

public sealed record ClinicalOrderResponse(
    Guid Id,
    Guid VisitId,
    Guid PatientId,
    Guid DoctorId,
    DateTime CreatedDate,
    IReadOnlyCollection<ClinicalOrderSectionResponse> Sections);

public sealed class ClinicalOrderLookupRequest
{
    public Guid VisitId { get; init; }
}

public sealed class SaveClinicalOrderRequest
{
    public Guid VisitId { get; init; }
    public IReadOnlyCollection<SaveClinicalOrderSectionRequest> Sections { get; init; } = [];
}

public sealed class SaveClinicalOrderSectionRequest
{
    public required string DefinitionCode { get; init; }
    public string? TextValue { get; init; }
    public IReadOnlyCollection<SaveClinicalOrderItemRequest> Items { get; init; } = [];
}

public sealed class SaveClinicalOrderItemRequest
{
    public required string Name { get; init; }
    public string? DetailsJson { get; init; }
}

public sealed class SaveClinicalOrderRequestValidator : AbstractValidator<SaveClinicalOrderRequest>
{
    public SaveClinicalOrderRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.Sections)
            .Must(sections => sections
                .Select(section => section.DefinitionCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == sections.Count)
            .WithMessage("Clinical order section definitions must be unique.");

        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(x => x.DefinitionCode).NotEmpty().MaximumLength(64);
            section.RuleFor(x => x.TextValue).MaximumLength(12000);
            section.RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.Name).NotEmpty().MaximumLength(512);
                item.RuleFor(x => x.DetailsJson).MaximumLength(12000);
            });
        });
    }
}
