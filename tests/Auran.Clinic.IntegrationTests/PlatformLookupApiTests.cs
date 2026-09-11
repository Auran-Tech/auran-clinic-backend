using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Lookups;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Auran.Clinic.IntegrationTests;

public sealed class PlatformLookupApiTests
{
    [Fact]
    public async Task LookupEndpoints_RejectAnonymousRequests()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var timeZonesResponse = await client.GetAsync("/api/platform/lookups/time-zones");
        var localesResponse = await client.GetAsync("/api/platform/lookups/locales");

        Assert.Equal(HttpStatusCode.Unauthorized, timeZonesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, localesResponse.StatusCode);
    }

    [Fact]
    public async Task LookupEndpoints_WithPlatformToken_ReturnSupportedOptions()
    {
        using var factory = new ApiFactory();
        var credentials = await CreatePlatformAccountAsync(factory);
        using var client = factory.CreateClient();
        var session = await PlatformLoginAsync(client, credentials);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var timeZonesResponse = await client.GetAsync("/api/platform/lookups/time-zones");
        var localesResponse = await client.GetAsync("/api/platform/lookups/locales");

        timeZonesResponse.EnsureSuccessStatusCode();
        localesResponse.EnsureSuccessStatusCode();

        var timeZonesEnvelope = await timeZonesResponse.Content
            .ReadFromJsonAsync<BaseResponse<List<TimeZoneLookupResponse>>>();
        var localesEnvelope = await localesResponse.Content
            .ReadFromJsonAsync<BaseResponse<List<LocaleLookupResponse>>>();

        Assert.True(timeZonesEnvelope?.Status);
        Assert.NotNull(timeZonesEnvelope?.Data);
        Assert.NotEmpty(timeZonesEnvelope.Data);
        Assert.Contains(timeZonesEnvelope.Data, timeZone =>
            timeZone.Id.Equals("Africa/Cairo", StringComparison.OrdinalIgnoreCase));

        Assert.True(localesEnvelope?.Status);
        Assert.NotNull(localesEnvelope?.Data);
        Assert.NotEmpty(localesEnvelope.Data);
        Assert.Contains(localesEnvelope.Data, locale =>
            locale.Code.Equals("en", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(localesEnvelope.Data, locale =>
            locale.Code.Equals("ar-EG", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<TestCredentials> CreatePlatformAccountAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"platform-lookup-{suffix}@auran.local";
        const string password = "ValidPassword1";

        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            AccountType = AccountType.Platform,
            LockoutEnabled = true
        };
        var identityResult = await userManager.CreateAsync(identityUser, password);
        Assert.True(identityResult.Succeeded, string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        dbContext.PlatformUsers.Add(new PlatformUser
        {
            Id = Guid.NewGuid(),
            IdentityUserId = identityUser.Id,
            FullName = $"Platform Lookup User {suffix}",
            Email = email,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        return new TestCredentials(email, password);
    }

    private static async Task<PlatformAuthResponse> PlatformLoginAsync(HttpClient client, TestCredentials credentials)
    {
        var response = await client.PostAsJsonAsync("/api/platform/auth/login", credentials.ToRequest());
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<PlatformAuthResponse>>();
        Assert.NotNull(envelope?.Data);
        return envelope.Data;
    }

    private sealed record TestCredentials(string Email, string Password)
    {
        public LoginRequest ToRequest() => new() { Email = Email, Password = Password };
    }
}
