using System.ComponentModel.DataAnnotations;
using Auran.Clinic.Api.Validation;

namespace Auran.Clinic.Api.Contracts.Clinics;

public abstract class ClinicProfileApiRequest
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

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

    [Required]
    [StringLength(100)]
    [SupportedReference(ReferenceDataKind.TimeZone)]
    public string? TimeZoneId { get; set; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [SupportedReference(ReferenceDataKind.Country)]
    public string? CountryCode { get; set; }

    [Required]
    [StringLength(20)]
    [SupportedReference(ReferenceDataKind.City, RelatedProperty = nameof(CountryCode))]
    public string? CityCode { get; set; }

    [Required]
    [CodePrefix]
    public string? PatientNumberPrefix { get; set; }

    [Required]
    [StringLength(20)]
    [SupportedReference(ReferenceDataKind.Locale)]
    public string? Locale { get; set; }

    [Required]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Required]
    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Website { get; set; }
}
