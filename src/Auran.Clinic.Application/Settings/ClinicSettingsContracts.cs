using FluentValidation;

namespace Auran.Clinic.Application.Settings;

public sealed record ClinicSettingsResponse(
    string ClinicName,
    string ClinicCode,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? FontFamily,
    string? WelcomeTitle,
    string? WelcomeMessage,
    string? TimeZoneId,
    string? PatientNumberPrefix,
    string? Phone,
    string? Email,
    string? Address,
    string? Website,
    string? Locale,
    string? DateFormat,
    string? TimeFormat,
    int DocumentationReminderHours,
    string? PrescriptionHeader,
    string? PrescriptionFooter,
    string? WelcomeButtonText);

public sealed class UpdateClinicSettingsRequest
{
    public required string ClinicName { get; init; }
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? FontFamily { get; init; }
    public string? WelcomeTitle { get; init; }
    public string? WelcomeMessage { get; init; }
    public string? TimeZoneId { get; init; }
    public string? PatientNumberPrefix { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Website { get; init; }
    public string? Locale { get; init; }
    public string? DateFormat { get; init; }
    public string? TimeFormat { get; init; }
    public int DocumentationReminderHours { get; init; } = 12;
    public string? PrescriptionHeader { get; init; }
    public string? PrescriptionFooter { get; init; }
    public string? WelcomeButtonText { get; init; }
}

public sealed class UpdateClinicSettingsRequestValidator : AbstractValidator<UpdateClinicSettingsRequest>
{
    public UpdateClinicSettingsRequestValidator()
    {
        RuleFor(x => x.ClinicName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.LogoUrl).MaximumLength(2000);
        RuleFor(x => x.PrimaryColor).MaximumLength(32);
        RuleFor(x => x.SecondaryColor).MaximumLength(32);
        RuleFor(x => x.FontFamily).MaximumLength(128);
        RuleFor(x => x.WelcomeTitle).MaximumLength(512);
        RuleFor(x => x.WelcomeMessage).MaximumLength(4000);
        RuleFor(x => x.TimeZoneId).MaximumLength(128);
        RuleFor(x => x.PatientNumberPrefix).MaximumLength(32);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(320);
        RuleFor(x => x.Address).MaximumLength(1000);
        RuleFor(x => x.Website).MaximumLength(1000);
        RuleFor(x => x.Locale).MaximumLength(16);
        RuleFor(x => x.DateFormat).MaximumLength(64);
        RuleFor(x => x.TimeFormat).MaximumLength(64);
        RuleFor(x => x.DocumentationReminderHours).InclusiveBetween(1, 168);
        RuleFor(x => x.PrescriptionHeader).MaximumLength(4000);
        RuleFor(x => x.PrescriptionFooter).MaximumLength(4000);
        RuleFor(x => x.WelcomeButtonText).MaximumLength(256);
    }
}
