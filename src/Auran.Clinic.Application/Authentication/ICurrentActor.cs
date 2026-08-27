using Auran.Clinic.Domain.Enums;

namespace Auran.Clinic.Application.Authentication;

public interface ICurrentActor
{
    bool IsAuthenticated { get; }
    ActorType ActorType { get; }
    string? IdentityUserId { get; }
    Guid? PlatformUserId { get; }
    Guid? ClinicUserId { get; }
    Guid? ClinicId { get; }
    bool IsClinicSuperUser { get; }
    string? DisplayName { get; }
    string? Email { get; }
}
