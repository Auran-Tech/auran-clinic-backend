using Auran.Clinic.Application.Visits;

namespace Auran.Clinic.UnitTests.Visits;

public sealed class VisitListQueryValidatorTests
{
    [Fact]
    public void Rejects_invalid_pagination_sort_and_date_range()
    {
        var validator = new VisitListQueryValidator();

        var result = validator.Validate(new VisitListQuery
        {
            Page = 0,
            PageSize = 100,
            SortBy = "clinicalNotes",
            FromUtc = new DateTime(2026, 10, 29),
            ToUtc = new DateTime(2026, 10, 28)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Page");
        Assert.Contains(result.Errors, error => error.PropertyName == "PageSize");
        Assert.Contains(result.Errors, error => error.PropertyName == "SortBy");
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("FromUtc"));
    }

    [Fact]
    public void Accepts_bounded_visit_filters()
    {
        var validator = new VisitListQueryValidator();

        var result = validator.Validate(new VisitListQuery
        {
            Page = 2,
            PageSize = 20,
            Search = "Patient",
            SortBy = "patientName"
        });

        Assert.True(result.IsValid);
    }
}
