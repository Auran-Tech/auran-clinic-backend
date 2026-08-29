using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auran.Clinic.Api.Contracts.Clinics;
using Auran.Clinic.Api.Contracts.Features;

namespace Auran.Clinic.IntegrationTests;

public sealed class ApiValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task InvalidAnonymousPost_ReturnsStandardBaseResponse()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.False(root.GetProperty("status").GetBoolean());
        Assert.Equal("Validation failed.", root.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("error").GetString()));
    }

    [Fact]
    public void CreateClinicApiContract_RequiresTransportFields()
    {
        var request = new CreateClinicApiRequest();
        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateClinicApiRequest.Name)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateClinicApiRequest.CodePrefix)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateClinicApiRequest.Admin)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateClinicApiRequest.TimeZoneId)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateClinicApiRequest.CountryCode)));
    }

    [Fact]
    public void FeatureApiContract_RejectsDuplicateCodes()
    {
        var request = new UpdateClinicFeaturesApiRequest
        {
            Features = new List<UpdateClinicFeatureApiRequest>
            {
                new() { Code = "AI", IsEnabled = true },
                new() { Code = "ai", IsEnabled = false }
            }
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.ErrorMessage == "Feature codes must be unique.");
    }

    [Fact]
    public void SetClinicStatusApiContract_RequiresExplicitValue()
    {
        var results = Validate(new SetClinicStatusApiRequest());

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(SetClinicStatusApiRequest.IsActive)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
