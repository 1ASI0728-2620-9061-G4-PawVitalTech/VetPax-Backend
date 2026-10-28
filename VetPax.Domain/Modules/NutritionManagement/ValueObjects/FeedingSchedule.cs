using VetPax.Domain.Modules.NutritionManagement.Exceptions;

namespace VetPax.Domain.Modules.NutritionManagement.ValueObjects;

public sealed class FeedingSchedule
{
    public Guid ScheduleId { get; }
    public TimeOnly LocalTime { get; }
    public FeedingPortion Portion { get; }
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }

    public FeedingSchedule(
        Guid scheduleId,
        TimeOnly localTime,
        FeedingPortion portion,
        IEnumerable<DayOfWeek> daysOfWeek)
    {
        if (scheduleId == Guid.Empty)
            throw new NutritionDomainException(
                "A valid feeding schedule ID is required.");

        ArgumentNullException.ThrowIfNull(portion);
        ArgumentNullException.ThrowIfNull(daysOfWeek);

        var days = daysOfWeek.Distinct().ToArray();

        if (days.Length == 0)
            throw new NutritionDomainException(
                "At least one feeding day is required.");

        if (days.Any(day => !Enum.IsDefined(day)))
            throw new NutritionDomainException(
                "Feeding schedule contains an invalid weekday.");

        ScheduleId = scheduleId;
        LocalTime = localTime;
        Portion = portion;
        DaysOfWeek = Array.AsReadOnly(days);
    }

    public bool AppliesOn(DateOnly localDate)
    {
        return DaysOfWeek.Contains(localDate.DayOfWeek);
    }
}