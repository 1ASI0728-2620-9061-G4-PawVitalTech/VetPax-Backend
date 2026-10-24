
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Entities;
using VetPax.Domain.Modules.MedicationTreatment.Enums;

namespace VetPax.Application.Modules.MedicationTreatment.Models;

public sealed record MedicationScheduleInput(
    TimeOnly LocalTime,
    IReadOnlyCollection<DayOfWeek> DaysOfWeek,
    decimal Amount,
    string Unit);

public sealed record CreateMedicationPlanCommand(
    Guid PetId,
    Guid ClinicId,
    Guid? ClinicalAttentionId,
    string MedicationName,
    string Route,
    string Instructions,
    DateOnly StartDate,
    DateOnly? EndDate,
    string TimeZoneId,
    IReadOnlyList<MedicationScheduleInput> Schedules);

public enum MedicationUpdateOperation
{
    ChangeSchedules,
    Complete,
    Cancel
}

public sealed record UpdateMedicationPlanCommand(
    Guid PlanId,
    MedicationUpdateOperation Operation,
    IReadOnlyList<MedicationScheduleInput>? Schedules = null);

public sealed record GetMedicationPlansQuery(Guid PetId);

public sealed record GetMedicationPlanQuery(Guid PlanId);

public sealed record ConfigureMedicationRemindersCommand(
    Guid PlanId,
    bool Enabled);

public sealed record GetMedicationDosesQuery(
    Guid PlanId,
    DateOnly? From = null,
    DateOnly? To = null);

public sealed record RegisterDoseAdministrationCommand(
    Guid DoseId,
    DateTime AdministeredAtUtc);

public sealed record GenerateMedicationDosesCommand(
    DateOnly ThroughUtcDate,
    int BatchSize = 20);

public sealed record MedicationScheduleData(
    Guid ScheduleId,
    TimeOnly LocalTime,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    decimal Amount,
    string Unit);

public sealed record MedicationDoseData(
    Guid DoseId,
    Guid PlanId,
    Guid ScheduleId,
    int ScheduleVersion,
    DateOnly LocalDate,
    TimeOnly LocalTime,
    DateTime ScheduledAtUtc,
    MedicationDoseStatus Status,
    DateTime? AdministeredAtUtc)
{
    public static MedicationDoseData From(MedicationDose dose)
    {
        return new MedicationDoseData(
            dose.DoseId.Value,
            dose.PlanId.Value,
            dose.ScheduleId.Value,
            dose.ScheduleVersion,
            dose.LocalDate,
            dose.LocalTime,
            dose.ScheduledAtUtc,
            dose.Status,
            dose.Administration?.AdministeredAtUtc);
    }
}

public sealed record MedicationPlanData(
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
    MedicationPlanStatus Status,
    bool RemindersEnabled,
    int ScheduleVersion,
    IReadOnlyList<MedicationScheduleData> Schedules)
{
    public static MedicationPlanData From(MedicationPlan plan)
    {
        return new MedicationPlanData(
            plan.PlanId.Value,
            plan.PetId.Value,
            plan.OwnerId.Value,
            plan.ClinicId.Value,
            plan.VeterinarianId.Value,
            plan.ClinicalAttentionId,
            plan.MedicationName,
            plan.Route,
            plan.Instructions,
            plan.StartDate,
            plan.EndDate,
            plan.TimeZoneId,
            plan.Status,
            plan.RemindersEnabled,
            plan.ScheduleVersion,
            plan.Schedules.Select(x =>
                new MedicationScheduleData(
                    x.ScheduleId.Value,
                    x.LocalTime,
                    x.DaysOfWeek.ToArray(),
                    x.Dosage.Amount,
                    x.Dosage.Unit))
                .ToArray());
    }
}

public sealed record MedicationAdministrationData(
    Guid AdministrationId,
    Guid DoseId,
    Guid OwnerId,
    DateTime AdministeredAtUtc,
    DateTime RecordedAtUtc)
{
    public static MedicationAdministrationData From(
        DoseAdministration administration)
    {
        return new MedicationAdministrationData(
            administration.AdministrationId.Value,
            administration.DoseId.Value,
            administration.AdministeredBy.Value,
            administration.AdministeredAtUtc,
            administration.RecordedAtUtc);
    }
}

public sealed record MedicationNotificationMessage(
    Guid MessageId,
    Guid OwnerId,
    Guid PlanId,
    Guid DoseId,
    DateTime ScheduledAtUtc,
    string Title,
    string Body);
