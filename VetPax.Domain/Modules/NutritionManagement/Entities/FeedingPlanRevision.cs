using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Domain.Modules.NutritionManagement.Entities;

public sealed class FeedingPlanRevision
{
    public int Version { get; }
    public string FoodName { get; }
    public string? Recommendations { get; }
    public DateOnly StartDate { get; }
    public DateOnly? EndDate { get; }
    public string TimeZoneId { get; }
    public IReadOnlyList<FeedingSchedule> Schedules { get; }
    public DateTime ReplacedAtUtc { get; }

    public FeedingPlanRevision(
        int version,
        string foodName,
        string? recommendations,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules,
        DateTime replacedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(schedules);

        if (version < 1)
            throw new ArgumentOutOfRangeException(nameof(version));

        if (replacedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Revision timestamp must be UTC.",
                nameof(replacedAtUtc));

        Version = version;
        FoodName = foodName;
        Recommendations = recommendations;
        StartDate = startDate;
        EndDate = endDate;
        TimeZoneId = timeZoneId;
        Schedules = Array.AsReadOnly(schedules.ToArray());
        ReplacedAtUtc = replacedAtUtc;
    }
}