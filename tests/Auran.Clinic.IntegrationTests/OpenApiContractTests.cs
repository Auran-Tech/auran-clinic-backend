using System.Text.Json;

namespace Auran.Clinic.IntegrationTests;

public sealed class OpenApiContractTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task SwaggerDocument_ExposesStableAuthenticationOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("Auth_Login", GetOperationId(paths, "/api/auth/login", "post"));
        Assert.Equal("Auth_RefreshToken", GetOperationId(paths, "/api/auth/refresh", "post"));
        Assert.Equal("Auth_Logout", GetOperationId(paths, "/api/auth/logout", "post"));
        Assert.Equal("PlatformAuth_Login", GetOperationId(paths, "/api/platform/auth/login", "post"));
        Assert.Equal("PlatformAuth_RefreshToken", GetOperationId(paths, "/api/platform/auth/refresh", "post"));
        Assert.Equal("PlatformAuth_Logout", GetOperationId(paths, "/api/platform/auth/logout", "post"));
    }

    [Fact]
    public async Task SwaggerDocument_MarksAnonymousAuthenticationOperationsAsAnonymous()
    {
        using var client = factory.CreateClient();
        var document = await GetDocumentAsync(client);
        var paths = document.GetProperty("paths");

        Assert.True(document.GetProperty("security").GetArrayLength() > 0);
        Assert.Equal(0, GetOperation(paths, "/api/auth/login", "post").GetProperty("security").GetArrayLength());
        Assert.Equal(0, GetOperation(paths, "/api/auth/refresh", "post").GetProperty("security").GetArrayLength());
        Assert.Equal(0, GetOperation(paths, "/api/platform/auth/login", "post").GetProperty("security").GetArrayLength());
        Assert.Equal(0, GetOperation(paths, "/api/platform/auth/refresh", "post").GetProperty("security").GetArrayLength());
    }

    [Fact]
    public async Task SwaggerDocument_ExposesStablePlatformClinicOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("PlatformClinics_Search", GetOperationId(paths, "/api/platform/clinics", "get"));
        Assert.Equal("PlatformClinics_Create", GetOperationId(paths, "/api/platform/clinics", "post"));
        Assert.Equal("PlatformClinics_GetById", GetOperationId(paths, "/api/platform/clinics/{id}", "get"));
        Assert.Equal("PlatformClinics_Update", GetOperationId(paths, "/api/platform/clinics/{id}", "put"));
        Assert.Equal("PlatformClinics_SetStatus", GetOperationId(paths, "/api/platform/clinics/{id}/status", "put"));
        Assert.Equal("PlatformClinicFeatures_Get", GetOperationId(paths, "/api/platform/clinics/{id}/features", "get"));
        Assert.Equal("PlatformClinicFeatures_Update", GetOperationId(paths, "/api/platform/clinics/{id}/features", "put"));
        Assert.Equal("PlatformAuditLogs_Search", GetOperationId(paths, "/api/platform/audit-logs", "get"));
    }

    [Fact]
    public async Task SwaggerDocument_ExposesStableClinicSelfServiceOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("Clinic_GetCurrent", GetOperationId(paths, "/api/clinic", "get"));
        Assert.Equal("ClinicSettings_Get", GetOperationId(paths, "/api/clinic/settings", "get"));
        Assert.Equal("ClinicSettings_Update", GetOperationId(paths, "/api/clinic/settings", "put"));
        Assert.Equal("ClinicFeatures_GetCurrent", GetOperationId(paths, "/api/clinic/features", "get"));
        Assert.Equal("AuditLogs_Search", GetOperationId(paths, "/api/audit-logs", "get"));
    }

    private static async Task<JsonElement> GetDocumentAsync(HttpClient client)
    {
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static JsonElement GetOperation(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method);

    private static string? GetOperationId(JsonElement paths, string path, string method) =>
        GetOperation(paths, path, method).GetProperty("operationId").GetString();
}
