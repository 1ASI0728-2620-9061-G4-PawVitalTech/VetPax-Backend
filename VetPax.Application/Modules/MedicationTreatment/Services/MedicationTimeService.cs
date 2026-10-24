
using VetPax.Application.Modules.MedicationTreatment.Exceptions;

namespace VetPax.Application.Modules.MedicationTreatment.Services;

public sealed class MedicationTimeService
{
    public TimeZoneInfo Resolve(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new MedicationValidationException(
                "A time zone is required.");
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new MedicationValidationException(
                $"Unknown time zone: {timeZoneId}");
        }
        catch (InvalidTimeZoneException)
        {
            throw new MedicationValidationException(
                $"Invalid time zone: {timeZoneId}");
        }
    }

    public DateOnly GetLocalDate(
        DateTime utcNow,
        string timeZoneId)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new MedicationValidationException(
                "Current time must be UTC.");
        }

        var zone = Resolve(timeZoneId);

        var local = TimeZoneInfo.ConvertTimeFromUtc(
            utcNow,
            zone);

        return DateOnly.FromDateTime(local);
    }

    public DateTime ConvertToUtc(
        DateOnly localDate,
        TimeOnly localTime,
        string timeZoneId)
    {
        var zone = Resolve(timeZoneId);

        var local = DateTime.SpecifyKind(
            localDate.ToDateTime(localTime),
            DateTimeKind.Unspecified);

        if (zone.IsInvalidTime(local))
        {
            throw new MedicationValidationException(
                "This local time does not exist in the specified time zone.");
        }

        if (zone.IsAmbiguousTime(local))
        {
            throw new MedicationValidationException(
                "This local time is ambiguous due to a clock change.");
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            local,
            zone);
    }
}
