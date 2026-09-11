namespace Auran.Clinic.Application.Clinics;

public sealed class SetClinicStatusRequest
{
    public Guid ClinicId { get; init; }
    public bool IsActive { get; init; }
}
