using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class UserAccountStatusApiTests
{
    [Fact]
    public async Task SetStatus_WithoutManageStatusPermission_ReturnsForbidden()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var caller = await CreateAccountAsync(factory, clinicId);
        var target = await CreateAccountAsync(factory, clinicId);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, caller.Credentials);

        var response = await SetStatusAsync(client, target.UserId, isActive: false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await GetUserIsActiveAsync(factory, target.UserId));
    }

    [Fact]
    public async Task SetStatus_ManagerCanDisableRegularUser_RevokesAccessAndRefreshSessions()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var manager = await CreateAccountAsync(factory, clinicId, grantManageStatus: true);
        var target = await CreateAccountAsync(factory, clinicId);
        using var managerClient = factory.CreateClient();
        using var targetClient = factory.CreateClient();
        var targetSession = await AuthenticateAsync(targetClient, target.Credentials);
        await AuthenticateAsync(managerClient, manager.Credentials);

        var response = await SetStatusAsync(managerClient, target.UserId, isActive: false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse<UserAccountStatusResponse>>();
        Assert.NotNull(envelope?.Data);
        Assert.Equal(target.UserId, envelope.Data.UserId);
        Assert.False(envelope.Data.IsActive);

        Assert.False(await GetUserIsActiveAsync(factory, target.UserId));
        Assert.False(await HasUnrevokedSessionAsync(factory, target.UserId));

        var accessResponse = await targetClient.GetAsync("/api/permissions/list");
        Assert.Equal(HttpStatusCode.Unauthorized, accessResponse.StatusCode);

        using var anonymousClient = factory.CreateClient();
        var refreshResponse = await anonymousClient.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = targetSession.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task SetStatus_ManagerCannotChangeProtectedSuperUser_ReturnsForbidden()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var manager = await CreateAccountAsync(factory, clinicId, grantManageStatus: true);
        var protectedSuperUser = await CreateAccountAsync(factory, clinicId, isSuperUser: true);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, manager.Credentials);

        var response = await SetStatusAsync(client, protectedSuperUser.UserId, isActive: false);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await GetUserIsActiveAsync(factory, protectedSuperUser.UserId));
    }

    [Fact]
    public async Task SetStatus_SuperUserCanDisableAnotherSuperUser()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var caller = await CreateAccountAsync(factory, clinicId, isSuperUser: true);
        var target = await CreateAccountAsync(factory, clinicId, isSuperUser: true);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, caller.Credentials);

        var response = await SetStatusAsync(client, target.UserId, isActive: false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await GetUserIsActiveAsync(factory, target.UserId));
    }

    [Fact]
    public async Task DisableSelf_WithoutManageStatusPermission_DisablesAccountAndRevokesSession()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var account = await CreateAccountAsync(factory, clinicId);
        using var client = factory.CreateClient();
        var session = await AuthenticateAsync(client, account.Credentials);

        var response = await client.PostAsync("/api/users/disable-self", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await GetUserIsActiveAsync(factory, account.UserId));
        Assert.False(await HasUnrevokedSessionAsync(factory, account.UserId));

        var accessResponse = await client.GetAsync("/api/permissions/list");
        Assert.Equal(HttpStatusCode.Unauthorized, accessResponse.StatusCode);

        using var anonymousClient = factory.CreateClient();
        var refreshResponse = await anonymousClient.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task SetStatus_CrossClinicTarget_ReturnsNotFound()
    {
        await using var factory = new ApiFactory();
        var callerClinicId = await CreateClinicAsync(factory);
        var targetClinicId = await CreateClinicAsync(factory);
        var manager = await CreateAccountAsync(factory, callerClinicId, grantManageStatus: true);
        var target = await CreateAccountAsync(factory, targetClinicId);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, manager.Credentials);

        var response = await SetStatusAsync(client, target.UserId, isActive: false);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(await GetUserIsActiveAsync(factory, target.UserId));
    }

    [Fact]
    public async Task SetStatus_CanReactivateUser_AllowsLoginAgain()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var manager = await CreateAccountAsync(factory, clinicId, grantManageStatus: true);
        var target = await CreateAccountAsync(factory, clinicId, isActive: false);
        using var managerClient = factory.CreateClient();
        await AuthenticateAsync(managerClient, manager.Credentials);

        var response = await SetStatusAsync(managerClient, target.UserId, isActive: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(await GetUserIsActiveAsync(factory, target.UserId));

        using var targetClient = factory.CreateClient();
        var loginResponse = await targetClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = target.Credentials.Email,
                Password = target.Credentials.Password
            });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task SetStatus_EmptyUserId_ReturnsBadRequest()
    {
        await using var factory = new ApiFactory();
        var clinicId = await CreateClinicAsync(factory);
        var manager = await CreateAccountAsync(factory, clinicId, grantManageStatus: true);
        using var client = factory.CreateClient();
        await AuthenticateAsync(client, manager.Credentials);

        var response = await SetStatusAsync(client, Guid.Empty, isActive: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> SetStatusAsync(HttpClient client, Guid userId, bool isActive) =>
        client.PutAsJsonAsync(
            "/api/users/status",
            new UpdateUserStatusRequest
            {
                UserId = userId,
                IsActive = isActive
            });

    private static async Task<AuthResponse> AuthenticateAsync(HttpClient client, TestCredentials credentials)
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
        Assert.NotNull(envelope?.Data);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            envelope.Data.AccessToken);
        return envelope.Data;
    }

    private static async Task<Guid> CreateClinicAsync(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinicId = Guid.NewGuid();

        dbContext.Clinics.Add(new DomainClinic
        {
            Id = clinicId,
            Name = $"Account Status Clinic {suffix}",
            Code = $"US-{suffix}",
            IsActive = true
        });
        await dbContext.SaveChangesAsync();
        return clinicId;
    }

    private static async Task<TestAccount> CreateAccountAsync(
        ApiFactory factory,
        Guid clinicId,
        bool isSuperUser = false,
        bool isActive = true,
        bool grantManageStatus = false)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var userId = Guid.NewGuid();
        var email = $"account-status-{suffix}@auran.local";
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
            Id = userId,
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Account Status {suffix}",
            Email = email,
            IsSuperUser = isSuperUser,
            IsActive = isActive,
            CreatedDate = DateTime.UtcNow
        });

        if (grantManageStatus)
        {
            var permission = await dbContext.Permissions
                .SingleAsync(item => item.Code == Permissions.Users.ManageStatus);
            var roleId = Guid.NewGuid();

            dbContext.Roles.Add(new Role
            {
                Id = roleId,
                Code = $"ACCOUNT_STATUS_MANAGER_{suffix}",
                Name = $"Account Status Manager {suffix}",
                IsSystem = false,
                CreatedDate = DateTime.UtcNow
            });
            dbContext.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permission.Id,
                CreatedDate = DateTime.UtcNow
            });
            dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                UserId = userId,
                RoleId = roleId,
                CreatedDate = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        return new TestAccount(userId, new TestCredentials(email, password));
    }

    private static async Task<bool> GetUserIsActiveAsync(ApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        return await dbContext.Users
            .IgnoreQueryFilters()
            .Where(user => user.Id == userId)
            .Select(user => user.IsActive)
            .SingleAsync();
    }

    private static async Task<bool> HasUnrevokedSessionAsync(ApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        return await dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .AnyAsync(token => token.UserId == userId && token.RevokedDate == null);
    }

    private sealed record TestAccount(Guid UserId, TestCredentials Credentials);
    private sealed record TestCredentials(string Email, string Password);
}
