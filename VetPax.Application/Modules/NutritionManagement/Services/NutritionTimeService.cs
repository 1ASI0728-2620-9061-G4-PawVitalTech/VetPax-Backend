using VetPax.Domain.Modules.NutritionManagement.Exceptions;

namespace VetPax.Application.Modules.NutritionManagement.Services;

public sealed class NutritionTimeService
{
    public void ValidateTimeZone(string timeZoneId)
    {
        _ = Resolve(timeZoneId);
    }

    public DateTime? TryConvertToUtc(
        DateOnly localDate,
        TimeOnly localTime,
        string timeZoneId)
    {
        var zone = Resolve(timeZoneId);

        var local = DateTime.SpecifyKind(
            localDate.ToDateTime(localTime),
            DateTimeKind.Unspecified);

        // On the spring DST transition, some local
        // times simply do not exist.
        if (zone.IsInvalidTime(local))
            return null;

        if (zone.IsAmbiguousTime(local))
        {
            // Choose the later UTC occurrence
            // to prevent double notifications.
            var offset = zone.GetAmbiguousTimeOffsets(local).Min();

            return DateTime.SpecifyKind(
                local - offset,
                DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    private static TimeZoneInfo Resolve(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new NutritionDomainException(
                "A valid time zone is required.");

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new NutritionDomainException(
                "The specified time zone is not supported.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new NutritionDomainException(
                "The specified time zone is invalid.");
        }
    }
}