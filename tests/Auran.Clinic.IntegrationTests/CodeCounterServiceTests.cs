using Auran.Clinic.Application.Codes;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class CodeCounterServiceTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GenerateAsync_ConcurrentClinicRequests_ReserveDistinctSequentialNumbers()
    {
        var clinicId = await CreateClinicAsync();
        var prefix = $"PT{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        const int reservationCount = 20;

        var reservations = Enumerable.Range(0, reservationCount)
            .Select(_ => GenerateClinicCodeAsync(clinicId, prefix))
            .ToArray();

        var codes = await Task.WhenAll(reservations);

        Assert.Equal(reservationCount, codes.Distinct(StringComparer.Ordinal).Count());

        var numbers = codes
            .Select(code => int.Parse(code[(code.LastIndexOf('-') + 1)..]))
            .OrderBy(number => number)
            .ToArray();

        Assert.Equal(Enumerable.Range(1, reservationCount), numbers);

        using var verificationScope = factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var counter = await dbContext.CodeCounters.SingleAsync(counter =>
            counter.Scope == CodeScope.Clinic &&
            counter.ClinicId == clinicId &&
            counter.CodeType == CodeType.Patient &&
            counter.Prefix == prefix &&
            counter.Year == DateTime.UtcNow.Year);

        Assert.Equal(reservationCount, counter.LastNumber);
    }

    private async Task<Guid> CreateClinicAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Code Counter {suffix}",
            Code = $"CC-{suffix}",
            CreatedDate = DateTime.UtcNow
        };

        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic.Id;
    }

    private async Task<string> GenerateClinicCodeAsync(Guid clinicId, string prefix)
    {
        using var scope = factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<ICodeGeneratorService>();
        return await generator.GenerateAsync(
            CodeScope.Clinic,
            clinicId,
            CodeType.Patient,
            prefix);
    }
}
