namespace VetPax.Domain.Modules.ClinicManagement.ValueObjects;

public sealed record DailyOpeningHours
{
    public DayOfWeek DayOfWeek { get; }
    public TimeOnly OpenTime { get; }
    public TimeOnly CloseTime { get; }

    public DailyOpeningHours(
        DayOfWeek dayOfWeek,
        TimeOnly openTime,
        TimeOnly closeTime)
    {
        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;

        if (!IsValid())
            throw new ArgumentException(
                "Invalid clinic opening hours.");
    }

    public bool IsValid() =>
        Enum.IsDefined(DayOfWeek) &&
        CloseTime > OpenTime;

    public bool OverlapsWith(DailyOpeningHours other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return DayOfWeek == other.DayOfWeek &&
               OpenTime < other.CloseTime &&
               other.OpenTime < CloseTime;
    }
}