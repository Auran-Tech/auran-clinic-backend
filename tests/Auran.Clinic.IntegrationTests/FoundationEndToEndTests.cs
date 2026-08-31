using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.IntegrationTests;

public sealed class FoundationEndToEndTests(EndToEndApiFactory factory)
    : IClassFixture<EndToEndApiFactory>
{
    [SqlIntegrationFact]
    public async Task PlatformProvisioningAndClinicAuthentication_FullLifecycle_Works()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clinicAdminEmail = $"clinic-admin-{suffix}@example.com";
        const string clinicAdminPassword = "Auran_Clinic_Admin_123!";

        using var platformClient = factory.CreateClient();
        var platformLogin = await platformClient.PostAsJsonAsync("/api/platform-auth/login", new
        {
            email = EndToEndApiFactory.PlatformAdminEmail,
            password = EndToEndApiFactory.PlatformAdminPassword
        });
        var platformData = await ReadDataAsync(platformLogin, HttpStatusCode.OK);
        var platformAccessToken = RequiredString(platformData, "accessToken");
        platformClient.DefaultRequestHeaders.Authorization = Bearer(platformAccessToken);

        var createClinic = await platformClient.PostAsJsonAsync("/api/platform-clinics/create", new
        {
            name = $"AURAN CI Clinic {suffix}",
            codePrefix = $"C{suffix}",
            patientNumberPrefix = $"P{suffix}",
            timeZoneId = "UTC",
            countryCode = "EG",
            cityCode = "CAI",
            locale = "en-EG",
            phone = "+201111111111",
            email = $"clinic-{suffix}@example.com",
            address = "Cairo, Egypt",
            admin = new
            {
                fullName = $"Clinic Admin {suffix}",
                email = clinicAdminEmail,
                phone = "+201222222222",
                password = clinicAdminPassword
            }
        });
        var clinicData = await ReadDataAsync(createClinic, HttpStatusCode.Created);
        var clinicId = clinicData.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, clinicId);
        Assert.StartsWith($"C{suffix.ToUpperInvariant()}-", RequiredString(clinicData, "code"), StringComparison.Ordinal);

        using var clinicClient = factory.CreateClient();
        var firstClinicLoginData = await LoginClinicAsync(clinicClient, clinicAdminEmail, clinicAdminPassword);
        var accessToken1 = RequiredString(firstClinicLoginData, "accessToken");
        var refreshToken1 = RequiredString(firstClinicLoginData, "refreshToken");
        Assert.False(firstClinicLoginData.GetProperty("user").GetProperty("isClinicSuperUser").GetBoolean());
        Assert.Contains(
            Permissions.Clinic.Users.ManageStatus,
            ReadStrings(firstClinicLoginData.GetProperty("user").GetProperty("permissions")));

        clinicClient.DefaultRequestHeaders.Authorization = Bearer(accessToken1);
        var me = await clinicClient.GetAsync("/api/auth/me");
        var meData = await ReadDataAsync(me, HttpStatusCode.OK);
        Assert.Equal(clinicId, meData.GetProperty("clinicId").GetGuid());

        var permissionCatalog = await clinicClient.GetAsync("/api/permissions/list");
        var permissionCatalogData = await ReadDataAsync(permissionCatalog, HttpStatusCode.OK);
        Assert.NotEmpty(permissionCatalogData.EnumerateArray());
        Assert.All(permissionCatalogData.EnumerateArray(), permission =>
        {
            var descriptions = permission.GetProperty("descriptions");
            Assert.True(descriptions.TryGetProperty("en", out var english) && !string.IsNullOrWhiteSpace(english.GetString()));
            Assert.True(descriptions.TryGetProperty("ar", out var arabic) && !string.IsNullOrWhiteSpace(arabic.GetString()));
        });

        var refresh = await clinicClient.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refreshToken1 });
        var refreshedData = await ReadDataAsync(refresh, HttpStatusCode.OK);
        var accessToken2 = RequiredString(refreshedData, "accessToken");
        var refreshToken2 = RequiredString(refreshedData, "refreshToken");
        Assert.NotEqual(accessToken1, accessToken2);
        Assert.NotEqual(refreshToken1, refreshToken2);

        clinicClient.DefaultRequestHeaders.Authorization = Bearer(accessToken1);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clinicClient.GetAsync("/api/auth/me")).StatusCode);

        clinicClient.DefaultRequestHeaders.Authorization = Bearer(accessToken2);
        Assert.Equal(HttpStatusCode.OK, (await clinicClient.GetAsync("/api/auth/me")).StatusCode);

        var logout = await clinicClient.PostAsJsonAsync("/api/auth/logout", new { refreshToken = refreshToken2 });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clinicClient.GetAsync("/api/auth/me")).StatusCode);

        var loginAfterLogout = await LoginClinicAsync(clinicClient, clinicAdminEmail, clinicAdminPassword);
        var accessToken3 = RequiredString(loginAfterLogout, "accessToken");
        clinicClient.DefaultRequestHeaders.Authorization = Bearer(accessToken3);

        var suspend = await platformClient.PutAsJsonAsync(
            $"/api/platform-clinics/set-status/{clinicId}",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clinicClient.GetAsync("/api/auth/me")).StatusCode);

        var activate = await platformClient.PutAsJsonAsync(
            $"/api/platform-clinics/set-status/{clinicId}",
            new { isActive = true });
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var loginAfterActivation = await LoginClinicAsync(clinicClient, clinicAdminEmail, clinicAdminPassword);
        Assert.NotNull(RequiredString(loginAfterActivation, "accessToken"));

        await PromoteToClinicSuperUserAsync(factory, clinicAdminEmail);
        var superUserLogin = await LoginClinicAsync(clinicClient, clinicAdminEmail, clinicAdminPassword);
        var superUser = superUserLogin.GetProperty("user");
        Assert.True(superUser.GetProperty("isClinicSuperUser").GetBoolean());

        var expectedClinicPermissions = SystemPermissionCatalog.Clinic
            .Select(x => x.Key)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var actualSuperUserPermissions = ReadStrings(superUser.GetProperty("permissions"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedClinicPermissions, actualSuperUserPermissions);

        var superUserAccessToken = RequiredString(superUserLogin, "accessToken");
        clinicClient.DefaultRequestHeaders.Authorization = Bearer(superUserAccessToken);
        var disableSelf = await clinicClient.PostAsync("/api/users/disable-self", content: null);
        Assert.Equal(HttpStatusCode.OK, disableSelf.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await clinicClient.GetAsync("/api/auth/me")).StatusCode);

        await AssertClinicUserInactiveAsync(factory, clinicAdminEmail);

        using var verificationFactory = new EndToEndApiFactory();
        using var verificationClient = verificationFactory.CreateClient();
        var disabledLogin = await verificationClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = clinicAdminEmail,
            password = clinicAdminPassword
        });
        Assert.Equal(HttpStatusCode.Unauthorized, disabledLogin.StatusCode);
    }

    private static async Task<JsonElement> LoginClinicAsync(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        return await ReadDataAsync(response, HttpStatusCode.OK);
    }

    private static async Task PromoteToClinicSuperUserAsync(EndToEndApiFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var user = await dbContext.Users.IgnoreQueryFilters().SingleAsync(x => x.Email == email);
        user.IsClinicSuperUser = true;
        await dbContext.SaveChangesAsync();
    }

    private static async Task AssertClinicUserInactiveAsync(EndToEndApiFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var isActive = await dbContext.Users.IgnoreQueryFilters()
            .Where(x => x.Email == email)
            .Select(x => x.IsActive)
            .SingleAsync();
        Assert.False(isActive);
    }

    private static AuthenticationHeaderValue Bearer(string accessToken) => new("Bearer", accessToken);

    private static string RequiredString(JsonElement element, string propertyName)
    {
        var value = element.GetProperty(propertyName).GetString();
        Assert.False(string.IsNullOrWhiteSpace(value));
        return value!;
    }

    private static string[] ReadStrings(JsonElement array) =>
        array.EnumerateArray().Select(x => x.GetString()!).ToArray();

    private static async Task<JsonElement> ReadDataAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expectedStatusCode,
            $"Expected HTTP {(int)expectedStatusCode} but received {(int)response.StatusCode}. Payload: {payload}");

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.True(root.GetProperty("status").GetBoolean(), payload);
        return root.GetProperty("data").Clone();
    }
}
