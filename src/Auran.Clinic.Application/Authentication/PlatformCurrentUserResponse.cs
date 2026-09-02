namespace Auran.Clinic.Application.Authentication;

public sealed class PlatformCurrentUserResponse
{
    public Guid PlatformUserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
}
