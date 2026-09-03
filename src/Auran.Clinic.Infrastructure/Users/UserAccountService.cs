using System.Data;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Users;

public sealed class UserAccountService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    UserManager<ApplicationIdentityUser> userManager,
    IAuditService auditService) : IUserAccountService
{
    public async Task<IReadOnlyList<UserAccountResponse>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out _, out _))
            return Array.Empty<UserAccountResponse>();

        var users = await dbContext.Users.AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(user => user.Id).ToArray();
        var assignments = userIds.Length == 0
            ? []
            : await dbContext.UserRoles.AsNoTracking()
                .Where(item => userIds.Contains(item.UserId))
                .ToListAsync(cancellationToken);

        var roleIds = assignments.Select(item => item.RoleId).Distinct().ToArray();
        var roles = roleIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Roles.AsNoTracking()
                .Where(role => roleIds.Contains(role.Id))
                .ToDictionaryAsync(role => role.Id, role => role.Code, cancellationToken);

        return users
            .Select(user => MapUser(
                user,
                assignments
                    .Where(item => item.UserId == user.Id && roles.ContainsKey(item.RoleId))
                    .Select(item => roles[item.RoleId])
                    .OrderBy(code => code)
                    .ToList()))
            .ToList();
    }

    public async Task<UserManagementResult> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId, out var clinicId))
            return new UserManagementResult(UserManagementOutcome.Unauthenticated);

        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
            return new UserManagementResult(UserManagementOutcome.ValidationError, Error: validationError);

        if (request.IsSuperUser && !currentUserContext.IsSuperUser)
            return new UserManagementResult(UserManagementOutcome.Forbidden, Error: "Only a Clinic Super User can create another Super User.");

        var roleResolution = await ResolveRolesAsync(request.Roles, cancellationToken);
        if (roleResolution.Error is not null)
            return new UserManagementResult(UserManagementOutcome.ValidationError, Error: roleResolution.Error);
        if (!request.IsSuperUser && roleResolution.Roles.Count == 0)
            return new UserManagementResult(UserManagementOutcome.ValidationError, Error: "At least one system role is required for a normal user.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(email) is not null)
            return new UserManagementResult(UserManagementOutcome.Conflict, Error: "Email is already in use.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var identityUser = new ApplicationIdentityUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = Clean(request.Phone),
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccountType = AccountType.Clinic
            };

            var identityResult = await userManager.CreateAsync(identityUser, request.Password);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new UserManagementResult(
                    UserManagementOutcome.ValidationError,
                    Error: string.Join("; ", identityResult.Errors.Select(error => error.Description)));
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                IdentityUserId = identityUser.Id,
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = Clean(request.Phone),
                IsSuperUser = request.IsSuperUser,
                IsActive = true,
                CreatedDate = now,
                CreateByUserId = currentUserId
            };
            dbContext.Users.Add(user);

            foreach (var role in roleResolution.Roles)
            {
                dbContext.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    UserId = user.Id,
                    RoleId = role.Id,
                    CreatedDate = now,
                    CreateByUserId = currentUserId
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(
                "User.Created",
                nameof(User),
                user.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["email"] = email,
                    ["isSuperUser"] = user.IsSuperUser,
                    ["roles"] = roleResolution.Roles.Select(role => role.Code).ToArray()
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new UserManagementResult(
                UserManagementOutcome.Success,
                MapUser(user, roleResolution.Roles.Select(role => role.Code).OrderBy(code => code).ToList()));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserManagementResult> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId, out _))
            return new UserManagementResult(UserManagementOutcome.Unauthenticated);

        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
            return new UserManagementResult(UserManagementOutcome.ValidationError, Error: "Full name and email are required.");

        var target = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (target is null)
            return new UserManagementResult(UserManagementOutcome.NotFound);
        if (IsProtectedSuperUser(target, currentUserId))
            return new UserManagementResult(UserManagementOutcome.Forbidden);

        var identityUser = await userManager.FindByIdAsync(target.IdentityUserId);
        if (identityUser is null || identityUser.AccountType != AccountType.Clinic)
            return new UserManagementResult(UserManagementOutcome.NotFound);

        var email = request.Email.Trim().ToLowerInvariant();
        var duplicateIdentity = await userManager.FindByEmailAsync(email);
        if (duplicateIdentity is not null && duplicateIdentity.Id != identityUser.Id)
            return new UserManagementResult(UserManagementOutcome.Conflict, Error: "Email is already in use.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            identityUser.Email = email;
            identityUser.UserName = email;
            identityUser.PhoneNumber = Clean(request.Phone);
            var identityResult = await userManager.UpdateAsync(identityUser);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new UserManagementResult(
                    UserManagementOutcome.ValidationError,
                    Error: string.Join("; ", identityResult.Errors.Select(error => error.Description)));
            }

            target.FullName = request.FullName.Trim();
            target.Email = email;
            target.Phone = Clean(request.Phone);
            target.UpdatedDate = DateTime.UtcNow;
            target.UpdatedByUserId = currentUserId;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "User.Updated",
                nameof(User),
                target.Id.ToString(),
                new Dictionary<string, object?> { ["email"] = email },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new UserManagementResult(
                UserManagementOutcome.Success,
                await MapUserAsync(target, cancellationToken));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserManagementResult> SetRolesAsync(
        Guid userId,
        SetUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId, out var clinicId))
            return new UserManagementResult(UserManagementOutcome.Unauthenticated);

        var target = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (target is null)
            return new UserManagementResult(UserManagementOutcome.NotFound);
        if (IsProtectedSuperUser(target, currentUserId))
            return new UserManagementResult(UserManagementOutcome.Forbidden);

        var roleResolution = await ResolveRolesAsync(request.Roles, cancellationToken);
        if (roleResolution.Error is not null)
            return new UserManagementResult(UserManagementOutcome.ValidationError, Error: roleResolution.Error);
        if (!target.IsSuperUser && roleResolution.Roles.Count == 0)
            return new UserManagementResult(UserManagementOutcome.ValidationError, Error: "At least one system role is required for a normal user.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await dbContext.UserRoles
                .Where(item => item.UserId == target.Id)
                .ToListAsync(cancellationToken);
            dbContext.UserRoles.RemoveRange(existing);

            var now = DateTime.UtcNow;
            foreach (var role in roleResolution.Roles)
            {
                dbContext.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    UserId = target.Id,
                    RoleId = role.Id,
                    CreatedDate = now,
                    CreateByUserId = currentUserId
                });
            }

            await dbContext.RefreshTokens
                .Where(token => token.UserId == target.Id && token.RevokedDate == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.RevokedDate, now),
                    cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(
                "User.RolesChanged",
                nameof(User),
                target.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["roles"] = roleResolution.Roles.Select(role => role.Code).ToArray()
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new UserManagementResult(
                UserManagementOutcome.Success,
                MapUser(target, roleResolution.Roles.Select(role => role.Code).OrderBy(code => code).ToList()));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserAccountStatusResult> SetStatusAsync(
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId, out _))
            return new UserAccountStatusResult(UserAccountStatusOutcome.Unauthenticated);

        var target = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == request.UserId, cancellationToken);
        if (target is null)
            return new UserAccountStatusResult(UserAccountStatusOutcome.NotFound);

        if (IsProtectedSuperUser(target, currentUserId))
            return new UserAccountStatusResult(UserAccountStatusOutcome.Forbidden);

        return await ApplyStatusAsync(target, request.IsActive, currentUserId, cancellationToken);
    }

    public async Task<UserAccountStatusResult> DisableCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var currentUserId, out _))
            return new UserAccountStatusResult(UserAccountStatusOutcome.Unauthenticated);

        var currentUser = await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == currentUserId, cancellationToken);
        if (currentUser is null)
            return new UserAccountStatusResult(UserAccountStatusOutcome.NotFound);

        return await ApplyStatusAsync(currentUser, isActive: false, currentUserId, cancellationToken);
    }

    private async Task<UserAccountStatusResult> ApplyStatusAsync(
        User user,
        bool isActive,
        Guid changedByUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        if (!isActive && user.IsSuperUser)
        {
            var activeSuperUserCount = await dbContext.Users
                .CountAsync(candidate => candidate.IsSuperUser && candidate.IsActive, cancellationToken);

            if (activeSuperUserCount <= 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new UserAccountStatusResult(UserAccountStatusOutcome.Conflict);
            }
        }

        user.IsActive = isActive;
        user.UpdatedDate = now;
        user.UpdatedByUserId = changedByUserId;

        if (!isActive)
        {
            await dbContext.RefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedDate == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.RevokedDate, now),
                    cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.WriteAsync(
            isActive ? "User.Activated" : "User.Deactivated",
            nameof(User),
            user.Id.ToString(),
            new Dictionary<string, object?> { ["isActive"] = isActive },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UserAccountStatusResult(
            UserAccountStatusOutcome.Success,
            new UserAccountStatusResponse(user.Id, user.IsActive));
    }

    private async Task<UserAccountResponse> MapUserAsync(User user, CancellationToken cancellationToken)
    {
        var roleIds = await dbContext.UserRoles.AsNoTracking()
            .Where(item => item.UserId == user.Id)
            .Select(item => item.RoleId)
            .ToListAsync(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => role.Code)
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);
        return MapUser(user, roles);
    }

    private async Task<(List<Role> Roles, string? Error)> ResolveRolesAsync(
        IReadOnlyCollection<string> requestedRoles,
        CancellationToken cancellationToken)
    {
        var normalizedCodes = requestedRoles
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var knownCodes = SystemRoleCatalog.All.Select(role => role.Code).ToHashSet(StringComparer.Ordinal);
        var unknownCodes = normalizedCodes.Where(code => !knownCodes.Contains(code)).ToArray();
        if (unknownCodes.Length > 0)
            return ([], $"Unknown system role(s): {string.Join(", ", unknownCodes)}.");

        var roles = normalizedCodes.Length == 0
            ? []
            : await dbContext.Roles
                .Where(role => normalizedCodes.Contains(role.Code) && role.IsSystem)
                .ToListAsync(cancellationToken);

        if (roles.Count != normalizedCodes.Length)
            return ([], "The system role catalog is not initialized.");

        return (roles, null);
    }

    private bool IsProtectedSuperUser(User target, Guid currentUserId) =>
        target.IsSuperUser && target.Id != currentUserId && !currentUserContext.IsSuperUser;

    private bool TryGetCurrentActor(out Guid userId, out Guid clinicId)
    {
        if (currentUserContext.IsAuthenticated &&
            currentUserContext.UserId is Guid currentUserId &&
            currentUserContext.ClinicId is Guid currentClinicId)
        {
            userId = currentUserId;
            clinicId = currentClinicId;
            return true;
        }

        userId = Guid.Empty;
        clinicId = Guid.Empty;
        return false;
    }

    private static UserAccountResponse MapUser(User user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.IsActive,
            user.IsSuperUser,
            roles);

    private static string? ValidateCreateRequest(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return "Full name is required.";
        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required.";
        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required.";
        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
