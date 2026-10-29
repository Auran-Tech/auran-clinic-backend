using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Time;

internal static class ClinicLocalDate
{
    public static async Task<TimeZoneInfo> GetTimeZoneAsync(
        AuranClinicDbContext dbContext,
        ICurrentUserContext currentUserContext,
        CancellationToken cancellationToken)
    {
        if (!currentUserContext.IsAuthenticated || currentUserContext.ClinicId is not Guid clinicId)
            return TimeZoneInfo.Utc;

        var timeZoneId = await dbContext.Clinics.AsNoTracking()
            .Where(clinic => clinic.Id == clinicId)
            .Select(clinic => clinic.TimeZoneId)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    public static DateTime StartUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
