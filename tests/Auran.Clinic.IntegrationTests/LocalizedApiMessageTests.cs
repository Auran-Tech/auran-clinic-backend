using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Models;

namespace Auran.Clinic.IntegrationTests;

public sealed class LocalizedApiMessageTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    public static TheoryData<string, string, string> SuccessCases => new()
    {
        { "GET", "Data retrieved successfully.", "تم استرجاع البيانات بنجاح." },
        { "POST", "Request completed successfully.", "تم تنفيذ الطلب بنجاح." },
        { "PUT", "Updated successfully.", "تم التحديث بنجاح." },
        { "DELETE", "Deleted successfully.", "تم الحذف بنجاح." }
    };

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public async Task SuccessResponse_AllSupportedHttpVerbs_ReturnEnglishMessageByDefault(
        string method,
        string expectedEnglishMessage,
        string _)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/_test/message-probe");

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(envelope);
        Assert.True(envelope.Status);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
        Assert.Equal(expectedEnglishMessage, envelope.Message);
    }

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public async Task SuccessResponse_AllSupportedHttpVerbs_ReturnArabicMessageWhenRequested(
        string method,
        string _,
        string expectedArabicMessage)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/_test/message-probe");
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ar"));

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.NotNull(envelope);
        Assert.True(envelope.Status);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Message));
        Assert.Equal(expectedArabicMessage, envelope.Message);
    }

    [Fact]
    public async Task ValidationFailure_ReturnsLocalizedArabicMessage()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new StringContent("{\"email\":\"user@example.com\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ar"));

        using var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.False(envelope.Status);
        Assert.Equal("validation_error", envelope.Error);
        Assert.Equal(
            "فشل التحقق من صحة البيانات. يرجى مراجعة البيانات المرسلة وتصحيح الحقول غير الصحيحة أو المطلوبة.",
            envelope.Message);
    }

    [Fact]
    public async Task UnauthorizedResponse_ReturnsLocalizedArabicEnvelope()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/lookups/time-zones");
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ar"));

        using var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.False(envelope.Status);
        Assert.Equal("unauthorized", envelope.Error);
        Assert.Equal(
            "يلزم تسجيل الدخول، أو أن رمز الوصول غير صالح أو منتهي الصلاحية.",
            envelope.Message);
    }

    [Fact]
    public async Task ControllerHardcodedEnglishMessage_IsReplacedForArabicClient()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = $"missing-{Guid.NewGuid():N}@auran.local",
                Password = "ValidPassword1"
            })
        };
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ar"));

        using var response = await client.SendAsync(request);
        var envelope = await response.Content.ReadFromJsonAsync<BaseResponse>();

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(envelope);
        Assert.Equal(
            "يلزم تسجيل الدخول، أو أن رمز الوصول غير صالح أو منتهي الصلاحية.",
            envelope.Message);
        Assert.DoesNotContain("Invalid email or password", envelope.Message, StringComparison.OrdinalIgnoreCase);
    }
}
