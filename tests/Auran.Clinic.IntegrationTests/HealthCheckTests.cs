namespace Auran.Clinic.IntegrationTests;

public class HealthCheckTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task LiveHealthCheck_ShouldReturnSuccess()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
    }
}
