using Auran.Clinic.Api.Infrastructure;
using Auran.Clinic.Application.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Auran.Clinic.IntegrationTests;

public class ExceptionHandlingTests
{
    [Fact]
    public async Task GlobalExceptionHandler_ReturnsSafeStandardResponse()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddLocalization(options => options.ResourcesPath = "Resources");
        using var services = serviceCollection.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Response.Body = new MemoryStream();

        var localizer = services.GetRequiredService<IStringLocalizer<ApiMessages>>();
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            localizer);
        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("sensitive implementation detail"),
            CancellationToken.None);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("internal_server_error", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive implementation detail", body, StringComparison.Ordinal);
    }
}
