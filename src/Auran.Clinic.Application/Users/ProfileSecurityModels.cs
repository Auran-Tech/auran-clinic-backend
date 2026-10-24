using FluentValidation;

namespace Auran.Clinic.Application.Users;

public sealed class ChangeOwnPasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

public sealed class ChangeOwnPasswordRequestValidator : AbstractValidator<ChangeOwnPasswordRequest>
{
    public ChangeOwnPasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty();
        RuleFor(request => request.NewPassword).NotEmpty();
    }
}

public sealed record OwnSessionResponse(
    Guid Id,
    DateTime CreatedDate,
    DateTime ExpiresDate,
    bool IsCurrent);

public sealed class RevokeOwnSessionRequest
{
    public Guid SessionId { get; init; }
}

public sealed class RevokeOwnSessionRequestValidator : AbstractValidator<RevokeOwnSessionRequest>
{
    public RevokeOwnSessionRequestValidator()
    {
        RuleFor(request => request.SessionId).NotEmpty();
    }
}
