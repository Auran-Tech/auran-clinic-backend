using Auran.Clinic.Application.Authentication;

namespace Auran.Clinic.UnitTests.Authentication;

public sealed class AuthenticationValidationTests
{
    [Fact]
    public async Task LoginValidator_ShouldRejectInvalidPayload()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest { Email = "not-an-email", Password = string.Empty };

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(LoginRequest.Email));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public async Task LoginValidator_ShouldAcceptValidPayload()
    {
        var validator = new LoginRequestValidator();
        var request = new LoginRequest { Email = "doctor@clinic.test", Password = "Password123" };

        var result = await validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RefreshTokenValidator_ShouldRejectEmptyToken()
    {
        var validator = new RefreshTokenRequestValidator();
        var request = new RefreshTokenRequest { RefreshToken = string.Empty };

        var result = await validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }
}
