using FluentValidation;

namespace Auran.Clinic.Application.Clinics;

public sealed class ClinicLookupRequest
{
    public Guid ClinicId { get; init; }
}

public sealed class ClinicLookupRequestValidator : AbstractValidator<ClinicLookupRequest>
{
    public ClinicLookupRequestValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
    }
}
