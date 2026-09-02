using System.Net;
using System.Text;

namespace Auran.Clinic.IntegrationTests;

public sealed class ApiValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Login_MissingRequiredProperty_ReturnsStandardValidationEnvelope()
    {
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"email\":\"user@example.com\"}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/auth/login", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"status\":false", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Validation failed", body, StringComparison.Ordinal);
        Assert.DoesNotContain("errors", body, StringComparison.OrdinalIgnoreCase);
    }
}
