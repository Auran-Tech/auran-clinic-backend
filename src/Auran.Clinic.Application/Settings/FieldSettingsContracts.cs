using Auran.Clinic.Domain.Enums;
using FluentValidation;

namespace Auran.Clinic.Application.Settings;

public sealed record FieldOptionSettingsResponse(
    Guid Id,
    string Label,
    string Value,
    int SortOrder);

public sealed record PatientProfileFieldSettingsResponse(
    Guid Id,
    string Label,
    DynamicFieldType FieldType,
    bool IsRequired,
    bool IsEnabled,
    int SortOrder,
    IReadOnlyCollection<FieldOptionSettingsResponse> Options);

public sealed record PatientProfileSectionSettingsResponse(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsSystem,
    bool IsEnabled,
    IReadOnlyCollection<PatientProfileFieldSettingsResponse> Fields);

public sealed record ClinicalFieldSettingsResponse(
    Guid Id,
    string Name,
    DynamicFieldType FieldType,
    string? Unit,
    bool IsEnabled,
    int SortOrder,
    IReadOnlyCollection<FieldOptionSettingsResponse> Options);

public sealed record FieldSettingsResponse(
    IReadOnlyCollection<PatientProfileSectionSettingsResponse> ProfileSections,
    IReadOnlyCollection<ClinicalFieldSettingsResponse> ClinicalFields);

public sealed class SaveFieldOptionSettingsRequest
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class SavePatientProfileFieldSettingsRequest
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public DynamicFieldType FieldType { get; init; }
    public bool IsRequired { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
    public IReadOnlyCollection<SaveFieldOptionSettingsRequest> Options { get; init; } = [];
}

public sealed class SavePatientProfileSectionSettingsRequest
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsEnabled { get; init; } = true;
    public IReadOnlyCollection<SavePatientProfileFieldSettingsRequest> Fields { get; init; } = [];
}

public sealed class SaveClinicalFieldSettingsRequest
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DynamicFieldType FieldType { get; init; }
    public string? Unit { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
    public IReadOnlyCollection<SaveFieldOptionSettingsRequest> Options { get; init; } = [];
}

public sealed class SaveFieldSettingsRequest
{
    public IReadOnlyCollection<SavePatientProfileSectionSettingsRequest> ProfileSections { get; init; } = [];
    public IReadOnlyCollection<SaveClinicalFieldSettingsRequest> ClinicalFields { get; init; } = [];
}

public sealed class SaveFieldSettingsRequestValidator : AbstractValidator<SaveFieldSettingsRequest>
{
    public SaveFieldSettingsRequestValidator()
    {
        RuleForEach(x => x.ProfileSections).ChildRules(section =>
        {
            section.RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
            section.RuleForEach(x => x.Fields).ChildRules(field =>
            {
                field.RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
                field.RuleForEach(x => x.Options).ChildRules(option =>
                {
                    option.RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
                    option.RuleFor(x => x.Value).NotEmpty().MaximumLength(256);
                });
            });
        });

        RuleForEach(x => x.ClinicalFields).ChildRules(field =>
        {
            field.RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
            field.RuleFor(x => x.Unit).MaximumLength(64);
            field.RuleForEach(x => x.Options).ChildRules(option =>
            {
                option.RuleFor(x => x.Label).NotEmpty().MaximumLength(256);
                option.RuleFor(x => x.Value).NotEmpty().MaximumLength(256);
            });
        });
    }
}
