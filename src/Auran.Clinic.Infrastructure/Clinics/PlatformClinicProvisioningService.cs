using System.Data;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.Infrastructure.Clinics;

public sealed class PlatformClinicProvisioningService(
    AuranClinicDbContext dbContext,
    UserManager<ApplicationIdentityUser> userManager,
    ICodeGeneratorService codeGeneratorService) : IPlatformClinicProvisioningService
{
    public async Task<ClinicProvisioningResult> ProvisionAsync(
        CreateClinicRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return Failure(ClinicProvisioningFailure.Validation, validationError);

        var adminEmail = request.Admin.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return Failure(ClinicProvisioningFailure.Conflict, "Admin email is already in use.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var clinicCode = await codeGeneratorService.GenerateAsync(
                CodeScope.Platform,
                clinicId: null,
                CodeType.Clinic,
                request.CodePrefix,
                cancellationToken);

            var identityUser = new ApplicationIdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                PhoneNumber = Clean(request.Admin.Phone),
                EmailConfirmed = true,
                LockoutEnabled = true,
                AccountType = AccountType.Clinic
            };

            var identityResult = await userManager.CreateAsync(identityUser, request.Admin.Password);
            if (!identityResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();

                var duplicateIdentity = identityResult.Errors.Any(error =>
                    error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
                return Failure(
                    duplicateIdentity ? ClinicProvisioningFailure.Conflict : ClinicProvisioningFailure.Validation,
                    string.Join("; ", identityResult.Errors.Select(error => error.Description)));
            }

            var clinic = new DomainClinic
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Code = clinicCode,
                IsActive = true,
                TimeZoneId = Clean(request.TimeZoneId),
                PatientNumberPrefix = NormalizePatientPrefix(request.PatientNumberPrefix),
                CreatedDate = now
            };

            var admin = new User
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                IdentityUserId = identityUser.Id,
                FullName = request.Admin.FullName.Trim(),
                Email = adminEmail,
                Phone = Clean(request.Admin.Phone),
                IsSuperUser = true,
                IsActive = true,
                CreatedDate = now
            };

            dbContext.Clinics.Add(clinic);
            dbContext.ClinicSettings.Add(new ClinicSettings
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                Email = NormalizeEmail(request.Email),
                Phone = Clean(request.Phone),
                Address = Clean(request.Address),
                DocumentationReminderHours = 12,
                CreatedDate = now
            });
            dbContext.Users.Add(admin);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new ClinicProvisioningResult
            {
                Succeeded = true,
                ClinicId = clinic.Id,
                ClinicCode = clinic.Code,
                AdminUserId = admin.Id
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private static string? Validate(CreateClinicRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return "Clinic name is required.";
        if (request.Name.Trim().Length > 200)
            return "Clinic name must be at most 200 characters.";
        if (string.IsNullOrWhiteSpace(request.CodePrefix))
            return "Clinic code prefix is required.";
        if (request.Admin is null)
            return "Initial clinic admin is required.";
        if (string.IsNullOrWhiteSpace(request.Admin.FullName))
            return "Initial admin full name is required.";
        if (string.IsNullOrWhiteSpace(request.Admin.Email))
            return "Initial admin email is required.";
        if (string.IsNullOrWhiteSpace(request.Admin.Password))
            return "Initial admin password is required.";

        var patientPrefix = NormalizePatientPrefix(request.PatientNumberPrefix);
        if (patientPrefix is { Length: > 20 })
            return "Patient number prefix must be at most 20 characters.";

        return null;
    }

    private static ClinicProvisioningResult Failure(ClinicProvisioningFailure failure, string error) => new()
    {
        Failure = failure,
        Error = error
    };

    private static string? NormalizePatientPrefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
