using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Domain.Modules.NutritionManagement.Exceptions;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;

namespace VetPax.Application.Modules.NutritionManagement.Services;

public sealed class NutritionScheduleFactory
{
    public IReadOnlyList<FeedingSchedule> Create(
        IReadOnlyList<FeedingScheduleInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
            throw new NutritionDomainException(
                "At least one feeding schedule is required.");

        var schedules = new List<FeedingSchedule>();

        foreach (var input in inputs)
        {
            if (input is null || input.DaysOfWeek is null)
                throw new NutritionDomainException(
                    "Invalid feeding schedule.");

            schedules.Add(new FeedingSchedule(
                Guid.NewGuid(),
                input.LocalTime,
                new FeedingPortion(
                    input.Quantity,
                    input.Unit),
                input.DaysOfWeek));
        }

        return schedules;
    }
}