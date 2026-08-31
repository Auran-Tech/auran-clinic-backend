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

public sealed class RefreshTokenFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ConcurrentRefresh_WithSameToken_AllowsOnlyOneRotation()
    {
        var clinicId = await CreateClinicAsync();
        var credentials = await CreateUserAsync(clinicId, "concurrent");
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, credentials);

        var refreshRequest = new RefreshTokenRequest { RefreshToken = session.RefreshToken };
        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/refresh", refreshRequest),
            client.PostAsJsonAsync("/api/auth/refresh", refreshRequest));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized));
    }

    [Fact]
    public async Task Logout_CannotRevokeAnotherUsersRefreshTokenInSameClinic()
    {
        var clinicId = await CreateClinicAsync();
        var userACredentials = await CreateUserAsync(clinicId, "owner-a");
        var userBCredentials = await CreateUserAsync(clinicId, "owner-b");
        using var client = factory.CreateClient();
        var userASession = await LoginAsync(client, userACredentials);
        var userBSession = await LoginAsync(client, userBCredentials);

        var logoutResponse = await LogoutAsync(
            client,
            userASession.AccessToken,
            userBSession.RefreshToken);
        logoutResponse.EnsureSuccessStatusCode();

        var ownerRefreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = userBSession.RefreshToken });

        Assert.Equal(HttpStatusCode.OK, ownerRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesAssociatedAccessTokenImmediately()
    {
        var clinicId = await CreateClinicAsync();
        var credentials = await CreateUserAsync(clinicId, "logout-session");
        using var client = factory.CreateClient();
        var session = await LoginAsync(client, credentials);

        var logoutResponse = await LogoutAsync(
            client,
            session.AccessToken,
            session.RefreshToken);
        logoutResponse.EnsureSuccessStatusCode();

        var reusedAccessTokenResponse = await LogoutAsync(
            client,
            session.AccessToken,
            session.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, reusedAccessTokenResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshRotation_InvalidatesOriginalAccessTokenAndKeepsReplacementActive()
    {
        var clinicId = await CreateClinicAsync();
        var credentials = await CreateUserAsync(clinicId, "rotated-session");
        using var client = factory.CreateClient();
        var originalSession = await LoginAsync(client, credentials);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = originalSession.RefreshToken });
        refreshResponse.EnsureSuccessStatusCode();
        var envelope = await refreshResponse.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
        Assert.NotNull(envelope?.Data);
        var replacementSession = envelope.Data;

        var originalAccessResponse = await LogoutAsync(
            client,
            originalSession.AccessToken,
            replacementSession.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, originalAccessResponse.StatusCode);

        var replacementAccessResponse = await LogoutAsync(
            client,
            replacementSession.AccessToken,
            replacementSession.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, replacementAccessResponse.StatusCode);
    }

    private async Task<Guid> CreateClinicAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Refresh Flow Clinic {suffix}",
            Code = $"RF-{suffix}"
        };

        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic.Id;
    }

    private async Task<TestCredentials> CreateUserAsync(Guid clinicId, string label)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"{label}-{suffix}@auran.local";
        const string password = "ValidPassword1";
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
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Refresh Flow {label}",
            Email = email
        });
        await dbContext.SaveChangesAsync();

        return new TestCredentials(email, password);
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, TestCredentials credentials)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = credentials.Email,
                Password = credentials.Password
            });
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.Status);
        Assert.NotNull(envelope.Data);
        return envelope.Data;
    }

    private static Task<HttpResponseMessage> LogoutAsync(
        HttpClient client,
        string accessToken,
        string refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client.SendAsync(request);
    }

    private sealed record TestCredentials(string Email, string Password);
}
