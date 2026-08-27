using Auran.Clinic.Application.Features;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Auran.Clinic.Infrastructure.Features;

public sealed class ClinicAccessService(
    AuranClinicDbContext dbContext,
    IDistributedCache cache) : IClinicAccessService
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<bool> IsClinicActiveAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        var key = StatusKey(clinicId);
        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (bool.TryParse(cached, out var value))
            return value;

        var isActive = await dbContext.Clinics.AsNoTracking()
            .Where(x => x.Id == clinicId)
            .Select(x => x.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        await cache.SetStringAsync(key, isActive.ToString(), CacheOptions, cancellationToken);
        return isActive;
    }

    public async Task<bool> IsFeatureEnabledAsync(
        Guid clinicId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = featureCode.Trim();
        var key = FeatureKey(clinicId, normalizedCode);
        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (bool.TryParse(cached, out var value))
            return value;

        var isEnabled = await (from clinicFeature in dbContext.ClinicFeatures.AsNoTracking()
                               join feature in dbContext.FeatureDefinitions.AsNoTracking()
                                   on clinicFeature.FeatureDefinitionId equals feature.Id
                               where clinicFeature.ClinicId == clinicId && feature.Code == normalizedCode
                               select clinicFeature.IsEnabled)
            .SingleOrDefaultAsync(cancellationToken);

        await cache.SetStringAsync(key, isEnabled.ToString(), CacheOptions, cancellationToken);
        return isEnabled;
    }

    public Task InvalidateClinicStatusAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(StatusKey(clinicId), cancellationToken);

    public Task InvalidateFeatureAsync(
        Guid clinicId,
        string featureCode,
        CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(FeatureKey(clinicId, featureCode.Trim()), cancellationToken);

    private static string StatusKey(Guid clinicId) => $"clinic:{clinicId}:active";
    private static string FeatureKey(Guid clinicId, string featureCode) =>
        $"clinic:{clinicId}:feature:{featureCode}";
}
