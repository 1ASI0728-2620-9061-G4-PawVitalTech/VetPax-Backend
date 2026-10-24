
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Domain.Modules.MedicationTreatment.Exceptions;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using Xunit;

namespace VetPax.Tests.Domain.MedicationTreatment;

public sealed class MedicationPlanTests
{
    private static readonly DateOnly StartDate =
        new(2026, 11, 2);

    private static readonly DateOnly EndDate =
        new(2026, 12, 31);

    private static readonly DateTime CreatedAtUtc =
        new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime MondayDoseUtc =
        new(2026, 11, 2, 13, 0, 0, DateTimeKind.Utc);

    private static MedicationSchedule CreateSchedule(
        TimeOnly? time = null,
        IEnumerable<DayOfWeek>? days = null)
    {
        return new MedicationSchedule(
            MedicationScheduleId.New(),
            time ?? new TimeOnly(8, 0),
            days ?? Enum.GetValues<DayOfWeek>(),
            new MedicationDosage(1m, "tablet"));
    }

    private static MedicationPlan CreatePlan(
        bool withSchedules = true)
    {
        var schedules = withSchedules
            ? new[] { CreateSchedule() }
            : Array.Empty<MedicationSchedule>();

        return MedicationPlan.Prescribe(
            new MedicationOwnerId(Guid.NewGuid()),
            new MedicationPetId(Guid.NewGuid()),
            new MedicationClinicId(Guid.NewGuid()),
            new MedicationVeterinarianId(Guid.NewGuid()),
            "Medicamento de prueba",
            "Oral",
            "Seguir indicaciones veterinarias",
            StartDate,
            EndDate,
            "America/Lima",
            schedules,
            CreatedAtUtc);
    }

    [Fact]
    public void Prescribe_ValidData_CreatesActivePlan()
    {
        var plan = CreatePlan();

        Assert.NotEqual(
            Guid.Empty,
            plan.PlanId.Value);

        Assert.Equal(
            MedicationPlanStatus.Active,
            plan.Status);

        Assert.Equal(1, plan.ScheduleVersion);
        Assert.False(plan.RemindersEnabled);
        Assert.Single(plan.Schedules);
        Assert.Empty(plan.Doses);

        Assert.Contains(
            plan.PullDomainEvents(),
            x => x is MedicationPlanPrescribedEvent);
    }

    [Fact]
    public void Prescribe_InvalidDates_ThrowsDomainException()
    {
        Assert.Throws<MedicationDomainException>(() =>
            MedicationPlan.Prescribe(
                new MedicationOwnerId(Guid.NewGuid()),
                new MedicationPetId(Guid.NewGuid()),
                new MedicationClinicId(Guid.NewGuid()),
                new MedicationVeterinarianId(Guid.NewGuid()),
                "Medicamento",
                "Oral",
                "",
                new DateOnly(2026, 12, 10),
                new DateOnly(2026, 12, 1),
                "America/Lima",
                new[] { CreateSchedule() },
                CreatedAtUtc));
    }

    [Fact]
    public void ScheduleDose_RepeatedGeneration_IsIdempotent()
    {
        var plan = CreatePlan();
        var schedule = plan.Schedules[0];

        var first = plan.ScheduleDose(
            schedule.ScheduleId,
            StartDate,
            MondayDoseUtc,
            CreatedAtUtc);

        var second = plan.ScheduleDose(
            schedule.ScheduleId,
            StartDate,
            MondayDoseUtc,
            CreatedAtUtc);

        Assert.Equal(first.DoseId, second.DoseId);
        Assert.Single(plan.Doses);

        Assert.Equal(
            MedicationDoseStatus.Pending,
            first.Status);

        Assert.Single(
            plan.PullDomainEvents()
                .OfType<MedicationDoseScheduledEvent>());
    }

    [Fact]
    public void ScheduleDose_WrongWeekday_IsRejected()
    {
        var mondayOnly = CreateSchedule(
            days: new[] { DayOfWeek.Monday });

        var plan = MedicationPlan.Prescribe(
            new MedicationOwnerId(Guid.NewGuid()),
            new MedicationPetId(Guid.NewGuid()),
            new MedicationClinicId(Guid.NewGuid()),
            new MedicationVeterinarianId(Guid.NewGuid()),
            "Medicamento",
            "Oral",
            "",
            StartDate,
            EndDate,
            "America/Lima",
            new[] { mondayOnly },
            CreatedAtUtc);

        Assert.Throws<MedicationDomainException>(() =>
            plan.ScheduleDose(
                mondayOnly.ScheduleId,
                StartDate.AddDays(1),
                MondayDoseUtc.AddDays(1),
                CreatedAtUtc));
    }

    [Fact]
    public void ConfigureReminders_WithoutSchedules_IsRejected()
    {
        var plan = CreatePlan(withSchedules: false);

        Assert.Throws<MedicationDomainException>(() =>
            plan.ConfigureReminders(
                true,
                CreatedAtUtc.AddMinutes(1)));

        Assert.False(plan.RemindersEnabled);
    }

    [Fact]
    public void ConfigureReminders_EnableAndDisable_EmitsEvents()
    {
        var plan = CreatePlan();

        plan.ConfigureReminders(
            true,
            CreatedAtUtc.AddMinutes(1));

        Assert.True(plan.RemindersEnabled);

        plan.ConfigureReminders(
            false,
            CreatedAtUtc.AddMinutes(2));

        Assert.False(plan.RemindersEnabled);

        var events = plan.PullDomainEvents()
            .OfType<MedicationRemindersConfiguredEvent>()
            .ToArray();

        Assert.Equal(2, events.Length);
        Assert.True(events[0].Enabled);
        Assert.False(events[1].Enabled);
    }

    [Fact]
    public void RegisterAdministration_ValidDose_RecordsOnce()
    {
        var plan = CreatePlan();

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            StartDate,
            MondayDoseUtc,
            CreatedAtUtc);

        var administeredAt = MondayDoseUtc.AddMinutes(5);

        var administration = plan.RegisterAdministration(
            dose.DoseId,
            plan.OwnerId,
            administeredAt,
            administeredAt,
            StartDate);

        Assert.Equal(
            MedicationDoseStatus.Administered,
            dose.Status);

        Assert.NotNull(dose.Administration);

        Assert.Equal(
            plan.OwnerId,
            administration.AdministeredBy);

        Assert.Equal(
            administeredAt,
            administration.AdministeredAtUtc);

        var events = plan.PullDomainEvents()
            .OfType<MedicationDoseAdministeredEvent>()
            .ToArray();

        Assert.Single(events);
        Assert.Equal(dose.DoseId.Value, events[0].DoseId);

        Assert.Throws<MedicationDomainException>(() =>
            plan.RegisterAdministration(
                dose.DoseId,
                plan.OwnerId,
                administeredAt,
                administeredAt.AddMinutes(1),
                StartDate));
    }

    [Fact]
    public void RegisterAdministration_OtherOwner_IsRejected()
    {
        var plan = CreatePlan();

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            StartDate,
            MondayDoseUtc,
            CreatedAtUtc);

        Assert.Throws<MedicationDomainException>(() =>
            plan.RegisterAdministration(
                dose.DoseId,
                new MedicationOwnerId(Guid.NewGuid()),
                MondayDoseUtc,
                MondayDoseUtc,
                StartDate));

        Assert.Equal(
            MedicationDoseStatus.Pending,
            dose.Status);
    }

    [Fact]
    public void ChangeSchedules_PreservesHistoryAndCancelsPending()
    {
        var plan = CreatePlan();

        var oldSchedule = plan.Schedules[0];

        var firstDose = plan.ScheduleDose(
            oldSchedule.ScheduleId,
            StartDate,
            MondayDoseUtc,
            CreatedAtUtc);

        var secondDose = plan.ScheduleDose(
            oldSchedule.ScheduleId,
            StartDate.AddDays(1),
            MondayDoseUtc.AddDays(1),
            CreatedAtUtc);

        plan.RegisterAdministration(
            firstDose.DoseId,
            plan.OwnerId,
            MondayDoseUtc,
            MondayDoseUtc,
            StartDate);

        var newSchedule = CreateSchedule(
            new TimeOnly(9, 0));

        plan.ChangeSchedules(
            new[] { newSchedule },
            MondayDoseUtc.AddHours(1));

        Assert.Equal(2, plan.ScheduleVersion);
        Assert.Single(plan.Schedules);

        Assert.Equal(
            MedicationDoseStatus.Administered,
            firstDose.Status);

        Assert.Equal(
            MedicationDoseStatus.Cancelled,
            secondDose.Status);

        var newDose = plan.ScheduleDose(
            newSchedule.ScheduleId,
            StartDate.AddDays(1),
            MondayDoseUtc.AddDays(1).AddHours(1),
            MondayDoseUtc.AddHours(1));

        Assert.Equal(2, newDose.ScheduleVersion);
        Assert.NotEqual(firstDose.DoseId, newDose.DoseId);
        Assert.Equal(3, plan.Doses.Count);
    }

    [Fact]
    public void Complete_CancelsPendingDoses()
    {
        var plan = CreatePlan();

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            StartDate,
            MondayDoseUtc,
            CreatedAtUtc);

        plan.Complete(CreatedAtUtc.AddHours(1));

        Assert.Equal(
            MedicationPlanStatus.Completed,
            plan.Status);

        Assert.False(plan.RemindersEnabled);

        Assert.Equal(
            MedicationDoseStatus.Cancelled,
            dose.Status);

        Assert.Throws<MedicationDomainException>(() =>
            plan.ScheduleDose(
                plan.Schedules[0].ScheduleId,
                StartDate.AddDays(1),
                MondayDoseUtc.AddDays(1),
                CreatedAtUtc.AddHours(2)));
    }

    [Fact]
    public void Schedule_OverlappingWeekdaysAtSameTime_IsRejected()
    {
        var first = CreateSchedule(
            new TimeOnly(8, 0),
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday });

        var second = CreateSchedule(
            new TimeOnly(8, 0),
            new[] { DayOfWeek.Tuesday });

        Assert.Throws<MedicationDomainException>(() =>
            MedicationPlan.Prescribe(
                new MedicationOwnerId(Guid.NewGuid()),
                new MedicationPetId(Guid.NewGuid()),
                new MedicationClinicId(Guid.NewGuid()),
                new MedicationVeterinarianId(Guid.NewGuid()),
                "Medicamento",
                "Oral",
                "",
                StartDate,
                EndDate,
                "America/Lima",
                new[] { first, second },
                CreatedAtUtc));
    }

    [Fact]
    public void Rehydrate_DoesNotCreateNewDomainEvents()
    {
        var original = CreatePlan();

        var restored = MedicationPlan.Rehydrate(
            original.PlanId,
            original.OwnerId,
            original.PetId,
            original.ClinicId,
            original.VeterinarianId,
            original.ClinicalAttentionId,
            original.MedicationName,
            original.Route,
            original.Instructions,
            original.StartDate,
            original.EndDate,
            original.TimeZoneId,
            original.Status,
            original.RemindersEnabled,
            original.ScheduleVersion,
            original.CreatedAtUtc,
            original.UpdatedAtUtc,
            original.Schedules.ToArray(),
            original.Doses.ToArray());

        Assert.Equal(original.PlanId, restored.PlanId);
        Assert.Empty(restored.PullDomainEvents());
    }
}
