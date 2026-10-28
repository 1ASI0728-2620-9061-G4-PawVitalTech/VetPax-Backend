
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Entities;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Application.Modules.NutritionManagement.Models;

public sealed record FeedingScheduleInput(
    TimeOnly LocalTime,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    decimal Quantity,
    string Unit);

public sealed record CreateFeedingPlanCommand(
    Guid PetId,
    Guid ClinicId,
    string FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    IReadOnlyList<FeedingScheduleInput> Schedules);

public sealed record UpdateFeedingPlanCommand(
    Guid PlanId,
    string FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    IReadOnlyList<FeedingScheduleInput> Schedules);

public sealed record DeactivateFeedingPlanCommand(Guid PlanId);

public sealed record GetFeedingPlanQuery(Guid PlanId);

public sealed record GetActiveFeedingPlanQuery(Guid PetId);

public sealed record ConfigureFeedingRemindersCommand(
    Guid PlanId,
    bool Enabled);

public sealed record GenerateFeedingRemindersCommand(
    DateOnly FromDate,
    DateOnly ThroughDate,
    int BatchSize = 20);

public sealed record FeedingScheduleData(
    Guid ScheduleId,
    TimeOnly LocalTime,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    decimal Quantity,
    string Unit);

public sealed record FeedingPlanRevisionData(
    int Version,
    string FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    DateTime ReplacedAtUtc,
    IReadOnlyList<FeedingScheduleData> Schedules);

public sealed record FeedingPlanData(
    Guid PlanId,
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    string FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    string Status,
    bool RemindersEnabled,
    int Version,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<FeedingScheduleData> Schedules,
    IReadOnlyList<FeedingPlanRevisionData> Revisions)
{
    public static FeedingPlanData From(FeedingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new FeedingPlanData(
            plan.PlanId.Value,
            plan.PetId,
            plan.ClinicId,
            plan.VeterinarianId,
            plan.FoodName,
            plan.Recommendations,
            plan.StartDate,
            plan.EndDate,
            plan.TimeZoneId,
            plan.Status.ToString(),
            plan.RemindersEnabled,
            plan.Version,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            plan.Schedules.Select(MapSchedule).ToArray(),
            plan.Revisions.Select(MapRevision).ToArray());
    }

    private static FeedingScheduleData MapSchedule(
        FeedingSchedule schedule)
    {
        return new FeedingScheduleData(
            schedule.ScheduleId,
            schedule.LocalTime,
            schedule.DaysOfWeek.ToArray(),
            schedule.Portion.Quantity,
            schedule.Portion.Unit);
    }

    private static FeedingPlanRevisionData MapRevision(
        FeedingPlanRevision revision)
    {
        return new FeedingPlanRevisionData(
            revision.Version,
            revision.FoodName,
            revision.Recommendations,
            revision.StartDate,
            revision.EndDate,
            revision.TimeZoneId,
            revision.ReplacedAtUtc,
            revision.Schedules.Select(MapSchedule).ToArray());
    }
}

public sealed record FeedingReminderOccurrence(
    Guid PlanId,
    Guid PetId,
    Guid ScheduleId,
    int PlanVersion,
    DateOnly LocalDate,
    DateTime ScheduledAtUtc);
