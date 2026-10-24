
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using System.Text.Json.Serialization;

namespace VetPax.Api.Modules.MedicationTreatment.Resources;

public sealed record MedicationScheduleRequest(
    TimeOnly LocalTime,
    IReadOnlyList<string>? DaysOfWeek,
    decimal Amount,
    string Unit);

public sealed record CreateMedicationPlanRequest(
    Guid ClinicId,
    Guid? ClinicalAttentionId,
    string MedicationName,
    string Route,
    string? Instructions,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    IReadOnlyList<MedicationScheduleRequest>? Schedules);

public sealed record UpdateMedicationPlanRequest(
    string Operation,
    IReadOnlyList<MedicationScheduleRequest>? Schedules);


public sealed record ConfigureMedicationRemindersRequest(
    [property: JsonRequired] bool Enabled);

public sealed record RegisterMedicationAdministrationRequest(
    [property: JsonRequired] DateTimeOffset AdministeredAtUtc);




public static class MedicationRequestMapper
{
    public static IReadOnlyList<MedicationScheduleInput> ToInputs(
        IReadOnlyList<MedicationScheduleRequest>? schedules)
    {
        if (schedules is null)
        {
            throw new MedicationValidationException(
                "Schedules are required.");
        }

        var result = new List<MedicationScheduleInput>();

        foreach (var schedule in schedules)
        {
            if (schedule is null ||
                schedule.DaysOfWeek is null ||
                schedule.DaysOfWeek.Count == 0)
            {
                throw new MedicationValidationException(
                    "Each schedule requires at least one weekday.");
            }

            var days = new List<DayOfWeek>();

            foreach (var dayName in schedule.DaysOfWeek)
            {
                if (string.IsNullOrWhiteSpace(dayName))
                {
                    throw new MedicationValidationException(
                        "Weekday names cannot be empty.");
                }

                // Se admiten nombres en inglés:
                // Monday, Tuesday, Wednesday, etc.
                // No se admiten números de enum.
                var validName = Enum
                    .GetNames<DayOfWeek>()
                    .FirstOrDefault(name =>
                        string.Equals(
                            name,
                            dayName.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (validName is null)
                {
                    throw new MedicationValidationException(
                        $"Invalid weekday: {dayName}.");
                }

                days.Add(Enum.Parse<DayOfWeek>(validName));
            }

            result.Add(new MedicationScheduleInput(
                schedule.LocalTime,
                days.Distinct().ToArray(),
                schedule.Amount,
                schedule.Unit));
        }

        return result;
    }

    public static MedicationUpdateOperation ParseOperation(
        string? operation)
    {
        return operation?.Trim().ToLowerInvariant() switch
        {
            "changeschedules" =>
                MedicationUpdateOperation.ChangeSchedules,

            "complete" =>
                MedicationUpdateOperation.Complete,

            "cancel" =>
                MedicationUpdateOperation.Cancel,

            _ => throw new MedicationValidationException(
                "Operation must be changeSchedules, complete or cancel.")
        };
    }
}
