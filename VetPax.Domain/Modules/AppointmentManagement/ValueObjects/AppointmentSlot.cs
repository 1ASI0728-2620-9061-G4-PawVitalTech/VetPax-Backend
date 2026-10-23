namespace VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

public sealed record AppointmentSlot
{
    public const int DurationMinutes = 30;

    public DateOnly Date { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public AppointmentSlot(
        DateOnly date,
        TimeOnly startTime)
    {
        if (date == default)
            throw new ArgumentException(
                "A valid appointment date is required.");

        if (startTime.Minute is not (0 or 30) ||
            startTime.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentException(
                "Appointments must start on a 30-minute boundary.");
        }

        var end = startTime.ToTimeSpan()
                  + TimeSpan.FromMinutes(DurationMinutes);

        if (end >= TimeSpan.FromDays(1))
        {
            throw new ArgumentException(
                "Appointment cannot extend beyond the day.");
        }

        Date = date;
        StartTime = startTime;
        EndTime = TimeOnly.FromTimeSpan(end);
    }

    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    public bool OverlapsWith(AppointmentSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Date == other.Date
               && StartTime < other.EndTime
               && other.StartTime < EndTime;
    }
}