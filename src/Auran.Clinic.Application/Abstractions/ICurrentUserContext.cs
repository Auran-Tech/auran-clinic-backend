namespace Auran.Clinic.Application.Abstractions;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? ClinicId { get; }

    bool IsSuperUser { get; }
}
