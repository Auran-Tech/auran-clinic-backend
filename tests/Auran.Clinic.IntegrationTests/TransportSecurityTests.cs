using System.Net;
using System.Net.Http.Json;

namespace Auran.Clinic.IntegrationTests;

public class TransportSecurityTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Cors_AllowsConfiguredDevelopmentOrigin()
    {
        using var client = CreateHttpsClient();
        using var request = CreatePreflightRequest("http://localhost:4200");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://localhost:4200", origins);
    }

    [Fact]
    public async Task Cors_DoesNotAllowUnconfiguredOrigin()
    {
        using var client = CreateHttpsClient();
        using var request = CreatePreflightRequest("https://untrusted.example");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Login_ReturnsTooManyRequestsAfterFiveAttemptsFromSameClient()
    {
        using var client = CreateHttpsClient();
        var payload = new
        {
            Email = "rate-limit-test@example.invalid",
            Password = "InvalidPassword123"
        };

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/auth/login", payload);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var rejectedResponse = await client.PostAsJsonAsync("/api/auth/login", payload);
        var body = await rejectedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.Contains("rate_limit_exceeded", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_UsesTrustedForwardedClientIpForRateLimitPartition()
    {
        using var client = CreateHttpsClient();
        var payload = new
        {
            Email = "forwarded-rate-limit-test@example.invalid",
            Password = "InvalidPassword123"
        };

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var request = CreateLoginRequest(payload, "198.51.100.10");
            using var response = await client.SendAsync(request);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        using var rejectedRequest = CreateLoginRequest(payload, "198.51.100.10");
        using var rejectedResponse = await client.SendAsync(rejectedRequest);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);

        using var differentClientRequest = CreateLoginRequest(payload, "198.51.100.11");
        using var differentClientResponse = await client.SendAsync(differentClientRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, differentClientResponse.StatusCode);
    }

    private HttpClient CreateHttpsClient()
    {
        return factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        return request;
    }

    private static HttpRequestMessage CreateLoginRequest(object payload, string forwardedClientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Forwarded-For", forwardedClientIp);
        request.Headers.Add("X-Forwarded-Proto", "https");
        return request;
    }
}
