using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Users;

namespace Auran.Clinic.UnitTests;

public sealed class ApiInputIdentifierValidatorTests
{
    [Fact]
    public async Task UpdateClinicRequest_EmptyClinicId_IsInvalid()
    {
        var result = await new UpdateClinicRequestValidator().ValidateAsync(new UpdateClinicRequest
        {
            ClinicId = Guid.Empty,
            Name = "Clinic"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateClinicRequest.ClinicId));
    }

    [Fact]
    public async Task SetClinicStatusRequest_EmptyClinicId_IsInvalid()
    {
        var result = await new SetClinicStatusRequestValidator().ValidateAsync(new SetClinicStatusRequest
        {
            ClinicId = Guid.Empty,
            IsActive = true
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SetClinicStatusRequest.ClinicId));
    }

    [Fact]
    public async Task UpdateUserRequest_EmptyUserId_IsInvalid()
    {
        var result = await new UpdateUserRequestValidator().ValidateAsync(new UpdateUserRequest
        {
            UserId = Guid.Empty,
            FullName = "Test User",
            Email = "user@example.com"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateUserRequest.UserId));
    }

    [Fact]
    public async Task SetUserRolesRequest_EmptyUserId_IsInvalid()
    {
        var result = await new SetUserRolesRequestValidator().ValidateAsync(new SetUserRolesRequest
        {
            UserId = Guid.Empty,
            Roles = ["ADMIN"]
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SetUserRolesRequest.UserId));
    }
}
