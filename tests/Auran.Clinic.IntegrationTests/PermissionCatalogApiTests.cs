using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class PermissionCatalogApiTests
{
    [Fact]
    public async Task List_WithoutBearerToken_ReturnsUnauthorized()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/permissions/list");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsStableLocalizedCatalogForAuthenticatedClinicUser()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var credentials = await CreateAccountAsync(factory);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = credentials.Email,
                Password = credentials.Password
            });
        loginResponse.EnsureSuccessStatusCode();

        var loginEnvelope = await loginResponse.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
        Assert.NotNull(loginEnvelope?.Data);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/permissions/list");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginEnvelope.Data.AccessToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content
            .ReadFromJsonAsync<BaseResponse<List<PermissionCatalogResponse>>>();
        Assert.NotNull(envelope?.Data);

        var attendanceCreateShift = Assert.Single(
            envelope.Data,
            permission => permission.Key == Permissions.Attendance.CreateShift);
        Assert.Equal("Attendance", attendanceCreateShift.Group);
        Assert.Equal("Create work schedules", attendanceCreateShift.Descriptions["en"]);
        Assert.Equal("انشاء مواعيد العمل", attendanceCreateShift.Descriptions["ar"]);
    }

    private static async Task<TestCredentials> CreateAccountAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinicId = Guid.NewGuid();
        var email = $"catalog-api-{suffix}@auran.local";
        const string password = "ValidPassword1";

        dbContext.Clinics.Add(new DomainClinic
        {
            Id = clinicId,
            Name = $"Permission Catalog Clinic {suffix}",
            Code = $"PC-{suffix}",
            IsActive = true
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
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Permission Catalog User {suffix}",
            Email = email,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        return new TestCredentials(email, password);
    }

    private sealed record TestCredentials(string Email, string Password);
}
