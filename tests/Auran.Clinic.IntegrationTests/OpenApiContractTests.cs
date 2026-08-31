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
        Assert.Equal("PlatformAuth_Login", GetOperationId(paths, "/api/platform-auth/login", "post"));
        Assert.Equal("PlatformAuth_RefreshToken", GetOperationId(paths, "/api/platform-auth/refresh", "post"));
        Assert.Equal("PlatformAuth_Logout", GetOperationId(paths, "/api/platform-auth/logout", "post"));
    }

    [Fact]
    public async Task SwaggerDocument_MarksAnonymousAuthenticationOperationsAsAnonymous()
    {
        using var client = factory.CreateClient();
        var document = await GetDocumentAsync(client);
        var paths = document.GetProperty("paths");

        Assert.True(document.GetProperty("security").GetArrayLength() > 0);
        AssertAnonymous(GetOperation(paths, "/api/auth/login", "post"));
        AssertAnonymous(GetOperation(paths, "/api/auth/refresh", "post"));
        AssertAnonymous(GetOperation(paths, "/api/platform-auth/login", "post"));
        AssertAnonymous(GetOperation(paths, "/api/platform-auth/refresh", "post"));
    }

    [Fact]
    public async Task SwaggerDocument_ExposesStablePlatformClinicOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("PlatformClinics_Search", GetOperationId(paths, "/api/platform-clinics/search", "get"));
        Assert.Equal("PlatformClinics_Create", GetOperationId(paths, "/api/platform-clinics/create", "post"));
        Assert.Equal("PlatformClinics_GetById", GetOperationId(paths, "/api/platform-clinics/get/{id}", "get"));
        Assert.Equal("PlatformClinics_Update", GetOperationId(paths, "/api/platform-clinics/update/{id}", "put"));
        Assert.Equal("PlatformClinics_SetStatus", GetOperationId(paths, "/api/platform-clinics/set-status/{id}", "put"));
        Assert.Equal("PlatformClinicFeatures_Get", GetOperationId(paths, "/api/platform-clinics/get-features/{id}", "get"));
        Assert.Equal("PlatformClinicFeatures_Update", GetOperationId(paths, "/api/platform-clinics/update-features/{id}", "put"));
        Assert.Equal("PlatformClinicBranding_CreateUploadSession", GetOperationId(paths, "/api/platform-clinics/create-branding-upload-session/{id}", "post"));
        Assert.Equal("PlatformClinicBranding_CompleteUploadSession", GetOperationId(paths, "/api/platform-clinics/complete-branding-upload-session/{id}/{sessionId}", "post"));
        Assert.Equal("PlatformAuditLogs_Search", GetOperationId(paths, "/api/platform-audit-logs/search", "get"));
    }

    [Fact]
    public async Task SwaggerDocument_ExposesStableClinicSelfServiceOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("Clinic_GetCurrent", GetOperationId(paths, "/api/clinic/current", "get"));
        Assert.Equal("ClinicSettings_Get", GetOperationId(paths, "/api/clinic/settings", "get"));
        Assert.Equal("ClinicSettings_Update", GetOperationId(paths, "/api/clinic/settings", "put"));
        Assert.Equal("ClinicFeatures_GetCurrent", GetOperationId(paths, "/api/clinic/features", "get"));
        Assert.Equal("AuditLogs_Search", GetOperationId(paths, "/api/audit-logs/search", "get"));
        Assert.Equal("Permissions_List", GetOperationId(paths, "/api/permissions/list", "get"));
        Assert.Equal("Users_UpdateStatus", GetOperationId(paths, "/api/users/status", "put"));
        Assert.Equal("Users_DisableSelf", GetOperationId(paths, "/api/users/disable-self", "post"));
    }

    [Fact]
    public async Task SwaggerDocument_ExposesAnonymousReferenceCatalogOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("Reference_Fonts", GetOperationId(paths, "/api/reference-data/fonts", "get"));
        Assert.Equal("Reference_Countries", GetOperationId(paths, "/api/reference-data/countries", "get"));
        Assert.Equal("Reference_Cities", GetOperationId(paths, "/api/reference-data/cities/{countryCode}", "get"));
        Assert.Equal("Reference_Locales", GetOperationId(paths, "/api/reference-data/locales", "get"));
        Assert.Equal("Reference_DateFormats", GetOperationId(paths, "/api/reference-data/date-formats", "get"));
        Assert.Equal("Reference_TimeFormats", GetOperationId(paths, "/api/reference-data/time-formats", "get"));
        Assert.Equal("Reference_TimeZones", GetOperationId(paths, "/api/reference-data/time-zones", "get"));
        AssertAnonymous(GetOperation(paths, "/api/reference-data/fonts", "get"));
    }

    [Fact]
    public async Task SwaggerDocument_ExposesStableFileUploadOperations()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");

        Assert.Equal("Files_CreateUploadSession", GetOperationId(paths, "/api/files/create-upload-session", "post"));
        Assert.Equal("Files_UploadContent", GetOperationId(paths, "/api/files/upload-content/{id}", "put"));
        Assert.Equal("Files_CompleteUploadSession", GetOperationId(paths, "/api/files/complete-upload-session/{id}", "post"));
        Assert.Equal("Files_Get", GetOperationId(paths, "/api/files/get/{id}", "get"));
        Assert.Equal("Files_Download", GetOperationId(paths, "/api/files/download/{id}", "get"));
        AssertAnonymous(GetOperation(paths, "/api/files/upload-content/{id}", "put"));
    }

    [Fact]
    public async Task ProtectedOperations_DocumentStructuredAuthorizationFailures()
    {
        using var client = factory.CreateClient();
        var paths = (await GetDocumentAsync(client)).GetProperty("paths");
        var operation = GetOperation(paths, "/api/platform-clinics/create", "post");
        var responses = operation.GetProperty("responses");

        Assert.True(responses.TryGetProperty("401", out var unauthorized));
        Assert.True(responses.TryGetProperty("403", out var forbidden));
        Assert.True(unauthorized.GetProperty("content").TryGetProperty("application/json", out _));
        Assert.True(forbidden.GetProperty("content").TryGetProperty("application/json", out _));
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsStructuredUnauthorizedResponse()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/platform-clinics/search");

        Assert.Equal(401, (int)response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.False(json.GetProperty("status").GetBoolean());
        Assert.Equal("Unauthorized.", json.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("error").GetString()));
    }

    private static async Task<JsonElement> GetDocumentAsync(HttpClient client)
    {
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static void AssertAnonymous(JsonElement operation)
    {
        var security = operation.GetProperty("security");
        Assert.True(security.GetArrayLength() > 0);
        Assert.Empty(security[0].EnumerateObject());
    }

    private static JsonElement GetOperation(JsonElement paths, string path, string method) =>
        paths.GetProperty(path).GetProperty(method);

    private static string? GetOperationId(JsonElement paths, string path, string method) =>
        GetOperation(paths, path, method).GetProperty("operationId").GetString();
}
