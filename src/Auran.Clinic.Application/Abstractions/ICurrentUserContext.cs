namespace Auran.Clinic.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid UserId { get; }

    Guid ClinicId { get; }

    bool IsSuperUser { get; }
}
