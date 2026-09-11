namespace Auran.Clinic.Application.Users;

public sealed class UpdateUserRequest
{
    public Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}
