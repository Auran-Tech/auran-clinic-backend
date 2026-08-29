using System.Text.Json;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.IntegrationTests;

public sealed class JsonSerializationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void ApiJsonOptions_SerializeEnumsAsNames()
    {
        var options = factory.Services
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .JsonSerializerOptions;

        var json = JsonSerializer.Serialize(new AuditLogResponse
        {
            Id = Guid.NewGuid(),
            Scope = AuditScope.Clinic,
            ActorType = ActorType.Platform,
            Action = "Test",
            Category = "Test",
            EntityType = "Test",
            OccurredAtUtc = DateTime.UtcNow
        }, options);

        Assert.Contains("\"scope\":\"Clinic\"", json);
        Assert.Contains("\"actorType\":\"Platform\"", json);
        Assert.DoesNotContain("\"scope\":1", json);
        Assert.DoesNotContain("\"actorType\":2", json);
    }
}
