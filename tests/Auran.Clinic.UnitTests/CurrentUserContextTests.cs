using System.Security.Claims;
using Auran.Clinic.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace Auran.Clinic.UnitTests;

public class CurrentUserContextTests
{
    [Fact]
    public void CurrentUser_MapsAuthenticatedClinicClaims()
    {
        var userId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", userId.ToString()),
            new Claim("clinic_id", clinicId.ToString()),
            new Claim("super_user", "true")
        ], "Bearer"));
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(clinicId, currentUser.ClinicId);
        Assert.True(currentUser.IsSuperUser);
    }

    [Fact]
    public void CurrentUser_ReturnsNullIdentifiersForMissingOrMalformedClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("user_id", "not-a-guid"),
            new Claim("super_user", "not-a-bool")
        ], "Bearer"));
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var currentUser = new CurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.ClinicId);
        Assert.False(currentUser.IsSuperUser);
    }

    [Fact]
    public void CurrentUser_IsUnauthenticatedWithoutHttpContext()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());

        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.UserId);
        Assert.Null(currentUser.ClinicId);
        Assert.False(currentUser.IsSuperUser);
    }
}
