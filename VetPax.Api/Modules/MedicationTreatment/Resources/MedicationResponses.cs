
using VetPax.Application.Modules.MedicationTreatment.Models;

namespace VetPax.Api.Modules.MedicationTreatment.Resources;

public sealed record MedicationScheduleResponse(
    Guid ScheduleId,
    TimeOnly LocalTime,
    IReadOnlyList<string> DaysOfWeek,
    decimal Amount,
    string Unit);

public sealed record MedicationPlanResponse(
    Guid PlanId,
    Guid PetId,
    Guid OwnerId,
    Guid ClinicId,
    Guid VeterinarianId,
    Guid? ClinicalAttentionId,
    string MedicationName,
    string Route,
    string Instructions,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    string Status,
    bool RemindersEnabled,
    int ScheduleVersion,
    IReadOnlyList<MedicationScheduleResponse> Schedules)
{
    public static MedicationPlanResponse From(
        MedicationPlanData plan)
    {
        return new MedicationPlanResponse(
            plan.PlanId,
            plan.PetId,
            plan.OwnerId,
            plan.ClinicId,
            plan.VeterinarianId,
            plan.ClinicalAttentionId,
            plan.MedicationName,
            plan.Route,
            plan.Instructions,
            plan.StartDate,
            plan.EndDate,
            plan.TimeZoneId,
            plan.Status.ToString(),
            plan.RemindersEnabled,
            plan.ScheduleVersion,
            plan.Schedules.Select(schedule =>
                new MedicationScheduleResponse(
                    schedule.ScheduleId,
                    schedule.LocalTime,
                    schedule.DaysOfWeek
                        .Select(day => day.ToString())
                        .ToArray(),
                    schedule.Amount,
                    schedule.Unit))
                .ToArray());
    }
}

public sealed record MedicationDoseResponse(
    Guid DoseId,
    Guid PlanId,
    Guid ScheduleId,
    int ScheduleVersion,
    DateOnly LocalDate,
    TimeOnly LocalTime,
    DateTime ScheduledAtUtc,
    string Status,
    DateTime? AdministeredAtUtc)
{
    public static MedicationDoseResponse From(
        MedicationDoseData dose)
    {
        return new MedicationDoseResponse(
            dose.DoseId,
            dose.PlanId,
            dose.ScheduleId,
            dose.ScheduleVersion,
            dose.LocalDate,
            dose.LocalTime,
            dose.ScheduledAtUtc,
            dose.Status.ToString(),
            dose.AdministeredAtUtc);
    }
}

public sealed record MedicationAdministrationResponse(
    Guid AdministrationId,
    Guid DoseId,
    Guid OwnerId,
    DateTime AdministeredAtUtc,
    DateTime RecordedAtUtc)
{
    public static MedicationAdministrationResponse From(
        MedicationAdministrationData administration)
    {
        return new MedicationAdministrationResponse(
            administration.AdministrationId,
            administration.DoseId,
            administration.OwnerId,
            administration.AdministeredAtUtc,
            administration.RecordedAtUtc);
    }
}
