using Auran.Clinic.Application.Patients;

namespace Auran.Clinic.UnitTests.Patients;

public sealed class PatientValidatorTests
{
    [Fact]
    public void Create_validator_rejects_missing_required_fields()
    {
        var validator = new CreatePatientRequestValidator();
        var result = validator.Validate(new CreatePatientRequest
        {
            FullName = "",
            Phone = ""
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Query_validator_limits_page_size()
    {
        var validator = new PatientQueryValidator();
        var result = validator.Validate(new PatientQuery
        {
            Page = 1,
            PageSize = 101
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_validator_rejects_future_date_of_birth()
    {
        var validator = new CreatePatientRequestValidator();
        var result = validator.Validate(new CreatePatientRequest
        {
            FullName = "Valid Patient",
            Phone = "+201001234567",
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        });

        Assert.False(result.IsValid);
    }
}
