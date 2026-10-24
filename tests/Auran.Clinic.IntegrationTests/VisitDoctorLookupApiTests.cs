using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Visits;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class VisitDoctorLookupApiTests
{
    [Fact]
    public async Task Receptionist_SeesOnlyActiveDoctorsInOwnClinic()
    {
        await using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();
        var clinicId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();
        var actors = new List<(User User, ApplicationIdentityUser Identity)>();
        var receptionistRole = await db.Roles.SingleAsync(role => role.Code == SystemRoleCatalog.Receptionist);
        var doctorRole = await db.Roles.SingleAsync(role => role.Code == SystemRoleCatalog.Doctor);
        const string password = "ValidPassword1!";

        db.Clinics.AddRange(
            new DomainClinic { Id = clinicId, Name = "Doctor lookup clinic", Code = $"DL-{clinicId:N}"[..15] },
            new DomainClinic { Id = otherClinicId, Name = "Other clinic", Code = $"DO-{otherClinicId:N}"[..15] });
        await db.SaveChangesAsync();

        async Task<User> AddUserAsync(Guid ownerClinicId, string name, Guid roleId, bool active = true)
        {
            var email = $"visit-doctor-{Guid.NewGuid():N}@example.invalid";
            var identity = new ApplicationIdentityUser { UserName = email, Email = email };
            Assert.True((await userManager.CreateAsync(identity, password)).Succeeded);
            var user = new User
            {
                Id = Guid.NewGuid(),
                ClinicId = ownerClinicId,
                IdentityUserId = identity.Id,
                FullName = name,
                Email = email,
                IsActive = active
            };
            db.Users.Add(user);
            db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                ClinicId = ownerClinicId,
                UserId = user.Id,
                RoleId = roleId
            });
            actors.Add((user, identity));
            return user;
        }

        try
        {
            var receptionist = await AddUserAsync(clinicId, "Receptionist", receptionistRole.Id);
            var available = await AddUserAsync(clinicId, "Available Doctor", doctorRole.Id);
            await AddUserAsync(clinicId, "Inactive Doctor", doctorRole.Id, active: false);
            await AddUserAsync(otherClinicId, "Foreign Doctor", doctorRole.Id);
            await db.SaveChangesAsync();

            using var client = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await client.GetAsync("/api/visits/doctors")).StatusCode);

            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = receptionist.Email!,
                Password = password
            });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var session = await login.Content.ReadFromJsonAsync<BaseResponse<AuthResponse>>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", session!.Data!.AccessToken);

            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.GetAsync("/api/users")).StatusCode);
            var response = await client.GetAsync("/api/visits/doctors");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<BaseResponse<List<VisitDoctorOptionResponse>>>();
            var doctor = Assert.Single(body!.Data!);
            Assert.Equal(available.Id, doctor.Id);
            Assert.Equal("Available Doctor", doctor.FullName);
        }
        finally
        {
            db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(token =>
                token.ClinicId == clinicId || token.ClinicId == otherClinicId));
            db.UserRoles.RemoveRange(db.UserRoles.Where(role =>
                role.ClinicId == clinicId || role.ClinicId == otherClinicId));
            db.Users.RemoveRange(db.Users.Where(user =>
                user.ClinicId == clinicId || user.ClinicId == otherClinicId));
            await db.SaveChangesAsync();
            db.Clinics.RemoveRange(db.Clinics.Where(clinic =>
                clinic.Id == clinicId || clinic.Id == otherClinicId));
            await db.SaveChangesAsync();
            foreach (var (_, identity) in actors)
                await userManager.DeleteAsync(identity);
        }
    }
}
