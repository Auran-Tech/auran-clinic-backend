using Auran.Clinic.Application.Clinics;

namespace Auran.Clinic.UnitTests;

public sealed class ClinicValidationTests
{
    [Fact]
    public void UpdateServiceContract_ExcludesImmutableProvisioningFields()
    {
        var propertyNames = typeof(UpdateClinicRequest)
            .GetProperties()
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain(nameof(CreateClinicRequest.CodePrefix), propertyNames);
        Assert.DoesNotContain(nameof(CreateClinicRequest.Admin), propertyNames);
        Assert.DoesNotContain("Code", propertyNames);
        Assert.DoesNotContain("IsActive", propertyNames);
    }

    [Fact]
    public void ServiceContracts_KeepRequiredCoreStrings()
    {
        Assert.True(IsRequiredMember<CreateClinicRequest>(nameof(CreateClinicRequest.Name)));
        Assert.True(IsRequiredMember<CreateClinicRequest>(nameof(CreateClinicRequest.CodePrefix)));
        Assert.True(IsRequiredMember<CreateClinicRequest>(nameof(CreateClinicRequest.TimeZoneId)));
        Assert.True(IsRequiredMember<UpdateClinicRequest>(nameof(UpdateClinicRequest.Name)));
        Assert.True(IsRequiredMember<UpdateClinicRequest>(nameof(UpdateClinicRequest.TimeZoneId)));
    }

    private static bool IsRequiredMember<T>(string propertyName) =>
        typeof(T).GetProperty(propertyName)!
            .CustomAttributes
            .Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");
}
