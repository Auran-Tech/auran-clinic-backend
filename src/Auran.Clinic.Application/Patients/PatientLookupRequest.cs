using FluentValidation;

namespace Auran.Clinic.Application.Patients;

public sealed class PatientLookupRequest
{
    public Guid PatientId { get; init; }
}

public sealed class PatientLookupRequestValidator : AbstractValidator<PatientLookupRequest>
{
    public PatientLookupRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
