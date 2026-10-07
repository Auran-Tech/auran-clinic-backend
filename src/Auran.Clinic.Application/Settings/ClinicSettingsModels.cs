using Auran.Clinic.Application.Lookups;

namespace Auran.Clinic.Application.Settings;

public sealed record ClinicSettingsResponse(
    Guid ClinicId,
    string ClinicName,
    string ClinicCode,
    bool IsActive,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? FontFamily,
    string? WelcomeTitle,
    string? WelcomeMessage,
    string? WelcomeButtonText,
    string? TimeZoneId,
    string? Locale,
    string? DateFormat,
    string? TimeFormat,
    string? PatientNumberPrefix,
    string? Phone,
    string? Email,
    string? Address,
    string? Website,
    int DocumentationReminderHours,
    string? PrescriptionHeader,
    string? PrescriptionFooter);

public sealed class UpdateClinicSettingsRequest
{
    public required string ClinicName { get; init; }
    public string? LogoUrl { get; init; }
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public string? FontFamily { get; init; }
    public string? WelcomeTitle { get; init; }
    public string? WelcomeMessage { get; init; }
    public string? WelcomeButtonText { get; init; }
    public string? TimeZoneId { get; init; }
    public string? Locale { get; init; }
    public string? DateFormat { get; init; }
    public string? TimeFormat { get; init; }
    public string? PatientNumberPrefix { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Website { get; init; }
    public int DocumentationReminderHours { get; init; }
    public string? PrescriptionHeader { get; init; }
    public string? PrescriptionFooter { get; init; }
}

public sealed record ClinicSettingsLookupsResponse(
    IReadOnlyList<TimeZoneLookupResponse> TimeZones,
    IReadOnlyList<LocaleLookupResponse> Locales);

public enum ClinicSettingsOutcome
{
    Success,
    NotFound,
    Unauthenticated
}

public sealed record ClinicSettingsResult(
    ClinicSettingsOutcome Outcome,
    ClinicSettingsResponse? Settings = null);
