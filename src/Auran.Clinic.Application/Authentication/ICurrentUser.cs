namespace Auran.Clinic.Application.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? ClinicId { get; }
    bool IsSuperUser { get; }
}
