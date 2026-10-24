using Auran.Clinic.Application.Users;

namespace Auran.Clinic.UnitTests;

public sealed class ProfileImageValidatorTests
{
    [Fact]
    public void AcceptsPngHeaderAndRejectsUnsupportedContent()
    {
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+XubcAAAAASUVORK5CYII=");

        Assert.True(ProfileImageValidator.IsValid(png, "image/png"));
        Assert.False(ProfileImageValidator.IsValid(png, "image/svg+xml"));
        Assert.False(ProfileImageValidator.IsValid("<svg>"u8, "image/png"));
        Assert.False(ProfileImageValidator.IsValid(new byte[ProfileImageValidator.MaxBytes + 1], "image/png"));
    }
}
