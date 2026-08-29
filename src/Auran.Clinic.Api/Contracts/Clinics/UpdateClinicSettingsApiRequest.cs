using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;

namespace Auran.Clinic.Api.Contracts.Clinics;

public sealed class UpdateClinicSettingsApiRequest
{
    [StringLength(1000)]
    public string? LogoUrl { get; set; }

    [HexColor]
    public string? PrimaryColor { get; set; }

    [HexColor]
    public string? SecondaryColor { get; set; }

    [StringLength(100)]
    [SupportedReference(ReferenceDataKind.Font)]
    public string? FontFamily { get; set; }

    [StringLength(200)]
    public string? WelcomeTitle { get; set; }

    [StringLength(2000)]
    public string? WelcomeMessage { get; set; }

    [StringLength(100)]
    public string? WelcomeButtonText { get; set; }

    [StringLength(100)]
    [SupportedReference(ReferenceDataKind.TimeZone)]
    public string? TimeZoneId { get; set; }

    [StringLength(2)]
    [SupportedReference(ReferenceDataKind.Country)]
    public string? CountryCode { get; set; }

    [StringLength(20)]
    [SupportedReference(ReferenceDataKind.City, RelatedProperty = nameof(CountryCode))]
    public string? CityCode { get; set; }

    [CodePrefix]
    public string? PatientNumberPrefix { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Website { get; set; }

    [StringLength(20)]
    [SupportedReference(ReferenceDataKind.Locale)]
    public string? Locale { get; set; }

    [StringLength(50)]
    [SupportedReference(ReferenceDataKind.DateFormat)]
    public string? DateFormat { get; set; }

    [StringLength(50)]
    [SupportedReference(ReferenceDataKind.TimeFormat)]
    public string? TimeFormat { get; set; }

    [Required]
    [Range(1, 168)]
    public int? DocumentationReminderHours { get; set; } = 12;

    [StringLength(2000)]
    public string? PrescriptionHeader { get; set; }

    [StringLength(2000)]
    public string? PrescriptionFooter { get; set; }
}
