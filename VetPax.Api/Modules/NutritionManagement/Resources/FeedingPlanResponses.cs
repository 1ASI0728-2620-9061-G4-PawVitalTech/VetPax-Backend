
using VetPax.Application.Modules.NutritionManagement.Models;

namespace VetPax.Api.Modules.NutritionManagement.Resources;

public sealed record FeedingScheduleResponse(
    Guid ScheduleId,
    TimeOnly LocalTime,
    IReadOnlyList<string> DaysOfWeek,
    decimal Quantity,
    string Unit);

public sealed record FeedingPlanRevisionResponse(
    int Version,
    string FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    DateTime ReplacedAtUtc,
    IReadOnlyList<FeedingScheduleResponse> Schedules);

public sealed record FeedingPlanResponse(
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
    IReadOnlyList<FeedingScheduleResponse> Schedules,
    IReadOnlyList<FeedingPlanRevisionResponse> Revisions)
{
    public static FeedingPlanResponse From(FeedingPlanData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new FeedingPlanResponse(
            data.PlanId,
            data.PetId,
            data.ClinicId,
            data.VeterinarianId,
            data.FoodName,
            data.Recommendations,
            data.StartDate,
            data.EndDate,
            data.TimeZoneId,
            data.Status,
            data.RemindersEnabled,
            data.Version,
            data.CreatedAtUtc,
            data.UpdatedAtUtc,
            data.Schedules.Select(MapSchedule).ToArray(),
            data.Revisions.Select(MapRevision).ToArray());
    }

    private static FeedingScheduleResponse MapSchedule(
        FeedingScheduleData data)
    {
        return new FeedingScheduleResponse(
            data.ScheduleId,
            data.LocalTime,
            data.DaysOfWeek
                .Select(day => day.ToString())
                .ToArray(),
            data.Quantity,
            data.Unit);
    }

    private static FeedingPlanRevisionResponse MapRevision(
        FeedingPlanRevisionData data)
    {
        return new FeedingPlanRevisionResponse(
            data.Version,
            data.FoodName,
            data.Recommendations,
            data.StartDate,
            data.EndDate,
            data.TimeZoneId,
            data.ReplacedAtUtc,
            data.Schedules.Select(MapSchedule).ToArray());
    }
}
