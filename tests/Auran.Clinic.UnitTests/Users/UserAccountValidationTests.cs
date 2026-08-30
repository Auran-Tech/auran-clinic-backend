using Auran.Clinic.Application.Users;

namespace Auran.Clinic.UnitTests.Users;

public sealed class UserAccountValidationTests
{
    [Fact]
    public async Task UpdateStatusValidator_ShouldRejectEmptyUserId()
    {
        var validator = new UpdateUserStatusRequestValidator();
        var request = new UpdateUserStatusRequest { UserId = Guid.Empty, IsActive = false };

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateUserStatusRequest.UserId));
    }

    [Fact]
    public async Task UpdateStatusValidator_ShouldAcceptValidUserId()
    {
        var validator = new UpdateUserStatusRequestValidator();
        var request = new UpdateUserStatusRequest { UserId = Guid.NewGuid(), IsActive = true };

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
