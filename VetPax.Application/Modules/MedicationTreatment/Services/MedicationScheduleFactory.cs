using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.Services;

public static class MedicationScheduleFactory
{
    public static IReadOnlyList<MedicationSchedule> Create(
        IReadOnlyList<MedicationScheduleInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        if (inputs.Count > 24)
        {
            throw new MedicationValidationException(
                "A medication plan cannot contain more than 24 schedules.");
        }

        var result = new List<MedicationSchedule>();

        foreach (var input in inputs)
        {
            if (input is null || input.DaysOfWeek is null)
            {
                throw new MedicationValidationException(
                    "Medication schedule information is incomplete.");
            }

            result.Add(new MedicationSchedule(
                MedicationScheduleId.New(),
                input.LocalTime,
                input.DaysOfWeek,
                new MedicationDosage(
                    input.Amount,
                    input.Unit)));
        }

        return result;
    }
}