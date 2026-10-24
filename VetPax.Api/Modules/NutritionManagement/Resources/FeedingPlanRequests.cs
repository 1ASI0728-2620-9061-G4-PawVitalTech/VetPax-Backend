
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Domain.Modules.NutritionManagement.Exceptions;

namespace VetPax.Api.Modules.NutritionManagement.Resources;

public sealed record FeedingScheduleRequest(
    TimeOnly LocalTime,
    IReadOnlyList<string>? DaysOfWeek,
    decimal Quantity,
    string? Unit);

public sealed record CreateFeedingPlanRequest(
    Guid ClinicId,
    string? FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? TimeZoneId,
    IReadOnlyList<FeedingScheduleRequest>? Schedules);

public sealed record UpdateFeedingPlanRequest(
    string? FoodName,
    string? Recommendations,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? TimeZoneId,
    IReadOnlyList<FeedingScheduleRequest>? Schedules);

public sealed record ConfigureFeedingRemindersRequest(
    bool? Enabled);

public static class FeedingPlanRequestMapper
{
    public static CreateFeedingPlanCommand ToCommand(
        Guid petId,
        CreateFeedingPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateFeedingPlanCommand(
            petId,
            request.ClinicId,
            request.FoodName ?? string.Empty,
            request.Recommendations,
            request.StartDate,
            request.EndDate,
            request.TimeZoneId ?? string.Empty,
            ToScheduleInputs(request.Schedules));
    }

    public static UpdateFeedingPlanCommand ToCommand(
        Guid planId,
        UpdateFeedingPlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateFeedingPlanCommand(
            planId,
            request.FoodName ?? string.Empty,
            request.Recommendations,
            request.StartDate,
            request.EndDate,
            request.TimeZoneId ?? string.Empty,
            ToScheduleInputs(request.Schedules));
    }

    public static IReadOnlyList<FeedingScheduleInput>
        ToScheduleInputs(
            IReadOnlyList<FeedingScheduleRequest>? schedules)
    {
        if (schedules is null || schedules.Count == 0)
        {
            throw new NutritionDomainException(
                "At least one feeding schedule is required.");
        }

        var inputs = new List<FeedingScheduleInput>();

        foreach (var schedule in schedules)
        {
            if (schedule is null)
            {
                throw new NutritionDomainException(
                    "A feeding schedule cannot be null.");
            }

            if (schedule.DaysOfWeek is null ||
                schedule.DaysOfWeek.Count == 0)
            {
                throw new NutritionDomainException(
                    "Each feeding schedule requires at least one day.");
            }

            var days = schedule.DaysOfWeek
                .Select(ParseDay)
                .Distinct()
                .ToArray();

            inputs.Add(new FeedingScheduleInput(
                schedule.LocalTime,
                days,
                schedule.Quantity,
                schedule.Unit ?? string.Empty));
        }

        return inputs;
    }

    private static DayOfWeek ParseDay(string? value)
    {
        var name = value?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new NutritionDomainException(
                "A feeding day cannot be empty.");
        }

        // Require weekday names, not numeric enum values.
        var validName = Enum.GetNames<DayOfWeek>()
            .Any(day => string.Equals(
                day,
                name,
                StringComparison.OrdinalIgnoreCase));

        if (!validName ||
            !Enum.TryParse<DayOfWeek>(
                name,
                true,
                out var parsed))
        {
            throw new NutritionDomainException(
                $"Invalid feeding day: {name}.");
        }

        return parsed;
    }
}
