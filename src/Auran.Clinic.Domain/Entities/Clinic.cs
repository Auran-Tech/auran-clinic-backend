using Auran.Clinic.Domain.Common;

namespace Auran.Clinic.Domain.Entities;

public class Clinic : BaseEntity
{
    public required string Name { get; set; }

    public required string Code { get; set; }

    public string? LogoUrl { get; set; }

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public string? FontFamily { get; set; }

    public string? WelcomeTitle { get; set; }

    public string? WelcomeMessage { get; set; }

    public string? TimeZoneId { get; set; }

    public string? PatientNumberPrefix { get; set; }
}
