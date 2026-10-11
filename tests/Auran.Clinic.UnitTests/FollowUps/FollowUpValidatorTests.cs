using Auran.Clinic.Application.FollowUps;

namespace Auran.Clinic.UnitTests.FollowUps;

public sealed class FollowUpValidatorTests
{
    [Fact]
    public void Create_requires_visit_recommendation_and_schedule()
    {
        var validator = new CreateFollowUpRequestValidator();

        var result = validator.Validate(new CreateFollowUpRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_accepts_recommended_date()
    {
        var validator = new CreateFollowUpRequestValidator();

        var result = validator.Validate(new CreateFollowUpRequest
        {
            VisitId = Guid.NewGuid(),
            Recommendation = "Review in clinic.",
            RecommendedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Status_change_requires_follow_up_id()
    {
        var validator = new ChangeFollowUpStatusRequestValidator();

        var result = validator.Validate(new ChangeFollowUpStatusRequest());

        Assert.False(result.IsValid);
    }
}
