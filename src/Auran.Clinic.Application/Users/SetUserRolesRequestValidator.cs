using FluentValidation;

namespace Auran.Clinic.Application.Users;

public sealed class SetUserRolesRequestValidator : AbstractValidator<SetUserRolesRequest>
{
    public SetUserRolesRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
    }
}
