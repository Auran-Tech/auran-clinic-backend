using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Auran.Clinic.Application.Authorization;

namespace Auran.Clinic.IntegrationTests;

public sealed class AuthenticationFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task SuperUser_Login_Me_And_Permissions_ShouldUseBackendEffectivePermissions()
    {
        using var client = factory.CreateClient();
        var accessToken = await LoginAsync(client, ApiFactory.SuperEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        using var meDocument = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        var user = meDocument.RootElement.GetProperty("data");
        Assert.True(user.GetProperty("isSuperUser").GetBoolean());
        var returnedPermissions = user.GetProperty("permissions")
            .EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => x is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(Permissions.All.Count, returnedPermissions.Count);
        Assert.All(Permissions.All, permission => Assert.Contains(permission.Key, returnedPermissions));

        var permissionResponse = await client.GetAsync("/api/permissions/list");
        permissionResponse.EnsureSuccessStatusCode();
        using var permissionDocument = JsonDocument.Parse(await permissionResponse.Content.ReadAsStringAsync());
        var permissions = permissionDocument.RootElement.GetProperty("data").EnumerateArray().ToList();
        Assert.Equal(Permissions.All.Count, permissions.Count);
        Assert.All(permissions, permission =>
        {
            var descriptions = permission.GetProperty("descriptions");
            Assert.True(descriptions.TryGetProperty("en", out _));
            Assert.True(descriptions.TryGetProperty("ar", out _));
        });
    }

    [Fact]
    public async Task NormalUser_ShouldBeForbiddenFromPermissionCatalog()
    {
        using var client = factory.CreateClient();
        var accessToken = await LoginAsync(client, ApiFactory.NormalEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/permissions/list");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DisabledUser_ShouldNotLogin()
    {
        await factory.SetUserActiveAsync(ApiFactory.NormalUserId, false);
        try
        {
            using var client = factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = ApiFactory.NormalEmail,
                password = ApiFactory.Password
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.SetUserActiveAsync(ApiFactory.NormalUserId, true);
        }
    }

    [Fact]
    public async Task DisabledClinic_ShouldRejectLoginAndExistingJwt()
    {
        using var client = factory.CreateClient();
        var accessToken = await LoginAsync(client, ApiFactory.SuperEmail);
        await factory.SetClinicActiveAsync(ApiFactory.ClinicAId, false);

        try
        {
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = ApiFactory.SuperEmail,
                password = ApiFactory.Password
            });
            Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var meResponse = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
        }
        finally
        {
            await factory.SetClinicActiveAsync(ApiFactory.ClinicAId, true);
        }
    }

    [Fact]
    public async Task DisableSelf_ShouldInvalidateExistingJwt()
    {
        using var client = factory.CreateClient();
        var accessToken = await LoginAsync(client, ApiFactory.NormalEmail);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var disableResponse = await client.PostAsync("/api/users/disable-self", null);
        disableResponse.EnsureSuccessStatusCode();

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        await factory.SetUserActiveAsync(ApiFactory.NormalUserId, true);
    }

    [Fact]
    public async Task InvalidLoginPayload_ShouldReturnValidationError()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "invalid",
            password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("error").GetString());
    }

    private static async Task<string> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = ApiFactory.Password
        });
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement
            .GetProperty("data")
            .GetProperty("accessToken")
            .GetString()!;
    }
}
