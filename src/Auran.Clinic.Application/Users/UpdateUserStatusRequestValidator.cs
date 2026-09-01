using FluentValidation;

namespace Auran.Clinic.Application.Users;

public sealed class UpdateUserStatusRequestValidator : AbstractValidator<UpdateUserStatusRequest>
{
    public UpdateUserStatusRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
    }
}
