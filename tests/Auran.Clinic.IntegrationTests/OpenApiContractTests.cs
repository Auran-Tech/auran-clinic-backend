using System.Text.Json;

namespace Auran.Clinic.IntegrationTests;

public sealed class OpenApiContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task SwaggerDocument_ExposesStableAuthenticationOperations()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal("Auth_Login", GetOperationId(paths, "/api/auth/login"));
        Assert.Equal("Auth_RefreshToken", GetOperationId(paths, "/api/auth/refresh"));
        Assert.Equal("Auth_Logout", GetOperationId(paths, "/api/auth/logout"));
    }

    private static string? GetOperationId(JsonElement paths, string path) =>
        paths.GetProperty(path).GetProperty("post").GetProperty("operationId").GetString();
}
