using FluentValidation;

namespace Auran.Clinic.Application.Users;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
    }
}
