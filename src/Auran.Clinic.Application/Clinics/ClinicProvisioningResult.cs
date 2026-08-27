namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicProvisioningResult
{
    public ClinicDetailsResponse? Clinic { get; init; }
    public string? Error { get; init; }
    public bool IsConflict { get; init; }
    public bool Succeeded => Clinic is not null;
}
