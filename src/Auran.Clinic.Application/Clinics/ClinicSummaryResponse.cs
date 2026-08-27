namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicSummaryResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; }
    public string? LogoUrl { get; set; }
    public string? TimeZoneId { get; set; }
    public string? PatientNumberPrefix { get; set; }
    public DateTime CreatedDate { get; set; }
}
