using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class AccountStateAuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Login_InactiveUser_IsRejected()
    {
        var fixture = await CreateAccountAsync(userIsActive: false, clinicIsActive: true);
        using var client = factory.CreateClient();

        var response = await LoginResponseAsync(client, fixture.Credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InactiveClinic_IsRejected()
    {
        var fixture = await CreateAccountAsync(userIsActive: true, clinicIsActive: false);
        using var client = factory.CreateClient();

        var response = await LoginResponseAsync(client, fixture.Credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterUserIsDisabled_IsRejected()
    {
        var fixture = await CreateAccountAsync(userIsActive: true, clinicIsActive: true);
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, fixture.Credentials);
        await SetUserStatusAsync(fixture.UserId, false);

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = session.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterClinicIsDisabled_IsRejected()
    {
        var fixture = await CreateAccountAsync(userIsActive: true, clinicIsActive: true);
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, fixture.Credentials);
        await SetClinicStatusAsync(fixture.ClinicId, false);

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = session.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExistingAccessToken_AfterUserIsDisabled_IsRejected()
    {
        var fixture = await CreateAccountAsync(userIsActive: true, clinicIsActive: true);
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, fixture.Credentials);
        await SetUserStatusAsync(fixture.UserId, false);

        var response = await LogoutResponseAsync(client, session);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExistingAccessToken_AfterClinicIsDisabled_IsRejected()
    {
        var fixture = await CreateAccountAsync(userIsActive: true, clinicIsActive: true);
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, fixture.Credentials);
        await SetClinicStatusAsync(fixture.ClinicId, false);

        var response = await LogoutResponseAsync(client, session);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<AccountFixture> CreateAccountAsync(bool userIsActive, bool clinicIsActive)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinicId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = $"state-{suffix}@auran.local";
        const string password = "ValidPassword1";

        dbContext.Clinics.Add(new DomainClinic
        {
            Id = clinicId,
            Name = $"Account State Clinic {suffix}",
            Code = $"AS-{suffix}",
            IsActive = clinicIsActive
        });

        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };
        var identityResult = await userManager.CreateAsync(identityUser, password);
        Assert.True(
            identityResult.Succeeded,
            string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        dbContext.Users.Add(new User
        {
            Id = userId,
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Account State {suffix}",
            Email = email,
            IsActive = userIsActive
        });
        await dbContext.SaveChangesAsync();

        return new AccountFixture(clinicId, userId, new TestCredentials(email, password));
    }

    private async Task SetUserStatusAsync(Guid userId, bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var user = await dbContext.Users.FindAsync(userId);
        Assert.NotNull(user);
        user.IsActive = isActive;
        await dbContext.SaveChangesAsync();
    }

    private async Task SetClinicStatusAsync(Guid clinicId, bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var clinic = await dbContext.Clinics.FindAsync(clinicId);
        Assert.NotNull(clinic);
        clinic.IsActive = isActive;
        await dbContext.SaveChangesAsync();
    }

    private static Task<HttpResponseMessage> LoginResponseAsync(HttpClient client, TestCredentials credentials) =>
        client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = credentials.Email,
                Password = credentials.Password
            });

    private static async Task<AuthResponse> LoginAsync(HttpClient client, TestCredentials credentials)
    {
        var response = await LoginResponseAsync(client, credentials);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Status);
        Assert.NotNull(envelope.Data);
        return envelope.Data;
    }

    private static Task<HttpResponseMessage> LogoutResponseAsync(HttpClient client, AuthResponse session)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new RefreshTokenRequest
            {
                RefreshToken = session.RefreshToken
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client.SendAsync(request);
    }

    private sealed record AccountFixture(Guid ClinicId, Guid UserId, TestCredentials Credentials);
    private sealed record TestCredentials(string Email, string Password);
}
