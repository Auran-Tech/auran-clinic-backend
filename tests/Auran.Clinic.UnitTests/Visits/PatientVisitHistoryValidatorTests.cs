using Auran.Clinic.Application.Visits;

namespace Auran.Clinic.UnitTests.Visits;

public sealed class PatientVisitHistoryValidatorTests
{
    [Fact]
    public void Query_requires_patient_id_and_valid_page()
    {
        var validator = new PatientVisitHistoryQueryValidator();

        var result = validator.Validate(new PatientVisitHistoryQuery
        {
            Page = 0,
            PageSize = 100
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Query_rejects_page_offset_that_would_overflow_int()
    {
        var validator = new PatientVisitHistoryQueryValidator();

        var result = validator.Validate(new PatientVisitHistoryQuery
        {
            PatientId = Guid.NewGuid(),
            Page = int.MaxValue,
            PageSize = 50
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("pagination range"));
    }

    [Fact]
    public void Query_accepts_valid_page()
    {
        var validator = new PatientVisitHistoryQueryValidator();

        var result = validator.Validate(new PatientVisitHistoryQuery
        {
            PatientId = Guid.NewGuid(),
            Page = 1,
            PageSize = 10
        });

        Assert.True(result.IsValid);
    }
}
