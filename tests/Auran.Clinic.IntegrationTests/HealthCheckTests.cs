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

    [Fact]
    public async Task ReadyHealthCheck_ShouldReturnSuccessWhenDatabaseIsReachable()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        response.EnsureSuccessStatusCode();
    }
}
