using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class PlatformAuthFlowTests
{
    [Fact]
    public async Task Login_PlatformAccount_Succeeds_AndClinicLoginRejectsSameCredentials()
    {
        using var factory = new ApiFactory();
        var credentials = await CreatePlatformAccountAsync(factory);
        using var client = factory.CreateClient();

        var platformResponse = await client.PostAsJsonAsync("/api/platform/auth/login", credentials.ToRequest());
        var clinicResponse = await client.PostAsJsonAsync("/api/auth/login", credentials.ToRequest());

        Assert.Equal(HttpStatusCode.OK, platformResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, clinicResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ClinicAccount_IsRejectedByPlatformEndpoint()
    {
        using var factory = new ApiFactory();
        var credentials = await CreateClinicAccountAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/platform/auth/login", credentials.ToRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RevokesPreviousPlatformAccessSession()
    {
        using var factory = new ApiFactory();
        var credentials = await CreatePlatformAccountAsync(factory);
        using var client = factory.CreateClient();
        var firstSession = await PlatformLoginAsync(client, credentials);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/platform/auth/refresh",
            new RefreshTokenRequest { RefreshToken = firstSession.RefreshToken });
        refreshResponse.EnsureSuccessStatusCode();
        var refreshedEnvelope = await refreshResponse.Content.ReadFromJsonAsync<BaseResponse<PlatformAuthResponse>>();
        Assert.NotNull(refreshedEnvelope?.Data);
        var secondSession = refreshedEnvelope.Data;

        var oldSessionResponse = await PlatformLogoutAsync(client, firstSession);
        var currentSessionResponse = await PlatformLogoutAsync(client, secondSession);

        Assert.Equal(HttpStatusCode.Unauthorized, oldSessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, currentSessionResponse.StatusCode);
    }

    [Fact]
    public async Task PlatformOnlyEndpoint_RejectsClinicAccessToken()
    {
        using var factory = new ApiFactory();
        var credentials = await CreateClinicAccountAsync(factory);
        using var client = factory.CreateClient();
        var clinicSession = await ClinicLoginAsync(client, credentials);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/auth/logout")
        {
            Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = clinicSession.RefreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clinicSession.AccessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClinicOnlyEndpoint_RejectsPlatformAccessToken()
    {
        using var factory = new ApiFactory();
        var credentials = await CreatePlatformAccountAsync(factory);
        using var client = factory.CreateClient();
        var platformSession = await PlatformLoginAsync(client, credentials);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/permissions/list");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", platformSession.AccessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<TestCredentials> CreatePlatformAccountAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"platform-{suffix}@auran.local";
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
            FullName = $"Platform User {suffix}",
            Email = email,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return new TestCredentials(email, password);
    }

    private static async Task<TestCredentials> CreateClinicAccountAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinicId = Guid.NewGuid();
        var email = $"clinic-actor-{suffix}@auran.local";
        const string password = "ValidPassword1";

        dbContext.Clinics.Add(new DomainClinic
        {
            Id = clinicId,
            Name = $"Actor Boundary Clinic {suffix}",
            Code = $"AB-{suffix}",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        });

        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            AccountType = AccountType.Clinic,
            LockoutEnabled = true
        };
        var identityResult = await userManager.CreateAsync(identityUser, password);
        Assert.True(identityResult.Succeeded, string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Clinic Actor {suffix}",
            Email = email,
            IsSuperUser = true,
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

    private static async Task<AuthResponse> ClinicLoginAsync(HttpClient client, TestCredentials credentials)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", credentials.ToRequest());
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
        Assert.NotNull(envelope?.Data);
        return envelope.Data;
    }

    private static Task<HttpResponseMessage> PlatformLogoutAsync(HttpClient client, PlatformAuthResponse session)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/auth/logout")
        {
            Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = session.RefreshToken })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client.SendAsync(request);
    }

    private sealed record TestCredentials(string Email, string Password)
    {
        public LoginRequest ToRequest() => new() { Email = Email, Password = Password };
    }
}
