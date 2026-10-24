using FluentValidation;

namespace Auran.Clinic.Application.Users;

public sealed class UpdateCurrentProfileRequest
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
}

public sealed class UpdateCurrentProfileRequestValidator : AbstractValidator<UpdateCurrentProfileRequest>
{
    public UpdateCurrentProfileRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
