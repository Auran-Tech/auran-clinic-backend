namespace Auran.Clinic.Application.Users;

public sealed class UpdateUserStatusRequest
{
    public Guid UserId { get; init; }

    public bool IsActive { get; init; }
}
