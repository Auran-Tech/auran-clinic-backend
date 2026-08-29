using Auran.Clinic.Application.Clinics;

namespace Auran.Clinic.UnitTests;

public sealed class ClinicValidationTests
{
    [Fact]
    public void UpdateContract_ExcludesImmutableClinicFields()
    {
        var propertyNames = typeof(UpdateClinicRequest)
            .GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Code", propertyNames);
        Assert.DoesNotContain("CodePrefix", propertyNames);
        Assert.DoesNotContain("Admin", propertyNames);
        Assert.DoesNotContain("IsActive", propertyNames);
    }

    [Fact]
    public async Task CreateValidator_RequiresCoreClinicProfile()
    {
        var validator = new CreateClinicRequestValidator();
        var request = new CreateClinicRequest
        {
            Name = "Clinic",
            CodePrefix = "CL",
            TimeZoneId = string.Empty,
            CountryCode = string.Empty,
            CityCode = string.Empty,
            PatientNumberPrefix = "PT",
            Locale = string.Empty,
            Phone = string.Empty,
            Email = string.Empty,
            Address = string.Empty,
            Admin = new InitialAdminRequest
            {
                FullName = "Admin",
                Email = "admin@example.com",
                Password = "Password1"
            }
        };

        var result = await validator.ValidateAsync(request);

        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.TimeZoneId));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.CountryCode));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.CityCode));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.Locale));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.Phone));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.Email));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(CreateClinicRequest.Address));
    }

    [Fact]
    public async Task UpdateValidator_RequiresSameMutableCoreProfile()
    {
        var validator = new UpdateClinicRequestValidator();
        var request = new UpdateClinicRequest
        {
            Name = "Clinic",
            TimeZoneId = string.Empty,
            CountryCode = string.Empty,
            CityCode = string.Empty,
            PatientNumberPrefix = "PT",
            Locale = string.Empty,
            Phone = string.Empty,
            Email = string.Empty,
            Address = string.Empty
        };

        var result = await validator.ValidateAsync(request);

        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.TimeZoneId));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.CountryCode));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.CityCode));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.Locale));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.Phone));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.Email));
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(UpdateClinicRequest.Address));
    }
}
