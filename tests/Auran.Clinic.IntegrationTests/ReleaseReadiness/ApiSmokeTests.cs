using System.Net;
using System.Net.Http.Json;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task Liveness_endpoint_returns_success()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patient_records_require_authentication()
    {
        var response = await _client.GetAsync("/api/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/visits/active?patientId=00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/visits/history?patientId=00000000-0000-0000-0000-000000000001")]
    public async Task Visit_records_require_authentication(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_route_uses_standard_json_error_contract()
    {
        var response = await _client.GetAsync("/api/route-that-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<BaseResponse>();
        Assert.NotNull(payload);
        Assert.False(payload.Status);
        Assert.Equal("not_found", payload.Error);
    }
}
