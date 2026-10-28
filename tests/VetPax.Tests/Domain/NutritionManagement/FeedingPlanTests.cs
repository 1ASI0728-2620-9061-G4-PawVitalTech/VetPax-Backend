
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Enums;
using VetPax.Domain.Modules.NutritionManagement.Events;
using VetPax.Domain.Modules.NutritionManagement.Exceptions;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Domain.NutritionManagement;

public sealed class FeedingPlanTests
{
    private static readonly DateTime Now =
        new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly StartDate =
        new(2026, 11, 2);

    private static readonly DateOnly EndDate =
        new(2026, 11, 9);

    [Fact]
    public void Prescribe_CreatesActivePlan()
    {
        var plan = CreatePlan();

        Assert.Equal(FeedingPlanStatus.Active, plan.Status);
        Assert.Equal(1, plan.Version);
        Assert.False(plan.RemindersEnabled);
        Assert.Equal("Veterinary food", plan.FoodName);
        Assert.Single(plan.Schedules);
        Assert.Empty(plan.Revisions);

        Assert.Contains(
            plan.PullDomainEvents(),
            e => e is FeedingPlanPrescribedEvent);
    }

    [Fact]
    public void Prescribe_RejectsMissingFood()
    {
        Assert.Throws<NutritionDomainException>(() =>
            FeedingPlan.Prescribe(
                FeedingPlanId.New(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                " ",
                null,
                StartDate,
                EndDate,
                "America/Lima",
                new[] { DailySchedule() },
                Now));
    }

    [Fact]
    public void Portion_RequiresPositiveQuantity()
    {
        Assert.Throws<NutritionDomainException>(() =>
            new FeedingPortion(0, "g"));

        Assert.Throws<NutritionDomainException>(() =>
            new FeedingPortion(-10, "g"));
    }

    [Fact]
    public void Portion_RequiresUnit()
    {
        Assert.Throws<NutritionDomainException>(() =>
            new FeedingPortion(100, " "));
    }

    [Fact]
    public void Schedule_RequiresValidDays()
    {
        Assert.Throws<NutritionDomainException>(() =>
            new FeedingSchedule(
                Guid.NewGuid(),
                new TimeOnly(8, 0),
                new FeedingPortion(150, "g"),
                Array.Empty<DayOfWeek>()));
    }

    [Fact]
    public void Prescribe_RejectsOverlappingSchedules()
    {
        var schedules = new[]
        {
            Schedule(
                new TimeOnly(8, 0),
                DayOfWeek.Monday),

            Schedule(
                new TimeOnly(8, 0),
                DayOfWeek.Monday)
        };

        Assert.Throws<NutritionDomainException>(() =>
            CreatePlan(schedules));
    }

    [Fact]
    public void Prescribe_AllowsDifferentMealTimes()
    {
        var plan = CreatePlan(new[]
        {
            DailySchedule(new TimeOnly(8, 0)),
            DailySchedule(new TimeOnly(18, 0))
        });

        Assert.Equal(2, plan.Schedules.Count);
    }

    [Fact]
    public void Update_PreservesPreviousVersion()
    {
        var plan = CreatePlan();
        plan.PullDomainEvents();

        plan.Update(
            "Updated veterinary food",
            "Serve under veterinary guidance.",
            StartDate,
            EndDate,
            "America/Lima",
            new[] { DailySchedule(new TimeOnly(9, 0)) },
            Now.AddMinutes(1));

        Assert.Equal(2, plan.Version);
        Assert.Equal(
            "Updated veterinary food",
            plan.FoodName);

        var revision = Assert.Single(plan.Revisions);

        Assert.Equal(1, revision.Version);
        Assert.Equal("Veterinary food", revision.FoodName);
        Assert.Equal(
            new TimeOnly(8, 0),
            Assert.Single(revision.Schedules).LocalTime);

        Assert.Contains(
            plan.PullDomainEvents(),
            e => e is FeedingPlanUpdatedEvent);
    }

    [Fact]
    public void InvalidUpdate_DoesNotChangePlan()
    {
        var plan = CreatePlan();
        plan.PullDomainEvents();

        Assert.Throws<NutritionDomainException>(() =>
            plan.Update(
                "",
                null,
                StartDate,
                EndDate,
                "America/Lima",
                new[] { DailySchedule() },
                Now.AddMinutes(1)));

        Assert.Equal(1, plan.Version);
        Assert.Equal("Veterinary food", plan.FoodName);
        Assert.Empty(plan.Revisions);
        Assert.Empty(plan.PullDomainEvents());
    }

    [Fact]
    public void ConfigureReminders_EnablesNotifications()
    {
        var plan = CreatePlan();
        plan.PullDomainEvents();

        plan.ConfigureReminders(
            true,
            Now.AddMinutes(1));

        Assert.True(plan.RemindersEnabled);

        Assert.Contains(
            plan.PullDomainEvents(),
            e => e is FeedingRemindersConfiguredEvent
            {
                Enabled: true
            });
    }

    [Fact]
    public void ConfigureReminders_SameValue_IsIdempotent()
    {
        var plan = CreatePlan();
        plan.PullDomainEvents();

        plan.ConfigureReminders(false, Now.AddMinutes(1));

        Assert.Empty(plan.PullDomainEvents());
    }

    [Fact]
    public void Reminder_RequiresScheduledDay()
    {
        var schedule = Schedule(
            new TimeOnly(8, 0),
            DayOfWeek.Monday);

        var plan = CreatePlan(new[] { schedule });

        plan.ConfigureReminders(true, Now.AddMinutes(1));

        Assert.True(plan.CanGenerateReminder(
            schedule.ScheduleId,
            new DateOnly(2026, 11, 2)));

        Assert.False(plan.CanGenerateReminder(
            schedule.ScheduleId,
            new DateOnly(2026, 11, 3)));
    }

    [Fact]
    public void Reminder_RejectsDateOutsidePlan()
    {
        var schedule = DailySchedule();
        var plan = CreatePlan(new[] { schedule });

        plan.ConfigureReminders(true, Now.AddMinutes(1));

        Assert.False(plan.CanGenerateReminder(
            schedule.ScheduleId,
            StartDate.AddDays(-1)));

        Assert.False(plan.CanGenerateReminder(
            schedule.ScheduleId,
            EndDate.AddDays(1)));
    }

    [Fact]
    public void GenerateReminder_EmitsDomainEvent()
    {
        var schedule = DailySchedule();
        var plan = CreatePlan(new[] { schedule });

        plan.ConfigureReminders(true, Now.AddMinutes(1));
        plan.PullDomainEvents();

        plan.RecordReminderGenerated(
            schedule.ScheduleId,
            StartDate,
            Now.AddMinutes(2));

        Assert.Contains(
            plan.PullDomainEvents(),
            e => e is FeedingReminderGeneratedEvent
            {
                PlanVersion: 1
            });
    }

    [Fact]
    public void Deactivate_PreventsNewReminders()
    {
        var schedule = DailySchedule();
        var plan = CreatePlan(new[] { schedule });

        plan.ConfigureReminders(true, Now.AddMinutes(1));
        plan.Deactivate(Now.AddMinutes(2));

        Assert.Equal(
            FeedingPlanStatus.Inactive,
            plan.Status);

        Assert.False(plan.RemindersEnabled);
        Assert.False(plan.CanGenerateReminder(
            schedule.ScheduleId,
            StartDate));

        Assert.Throws<NutritionDomainException>(() =>
            plan.RecordReminderGenerated(
                schedule.ScheduleId,
                StartDate,
                Now.AddMinutes(3)));
    }

    [Fact]
    public void InactivePlan_CannotBeUpdated()
    {
        var plan = CreatePlan();

        plan.Deactivate(Now.AddMinutes(1));

        Assert.Throws<NutritionDomainException>(() =>
            plan.Update(
                "Another food",
                null,
                StartDate,
                EndDate,
                "America/Lima",
                new[] { DailySchedule() },
                Now.AddMinutes(2)));
    }

    [Fact]
    public void Rehydrate_DoesNotEmitEvents()
    {
        var original = CreatePlan();

        var restored = FeedingPlan.Rehydrate(
            original.PlanId,
            original.PetId,
            original.ClinicId,
            original.VeterinarianId,
            original.FoodName,
            original.Recommendations,
            original.StartDate,
            original.EndDate,
            original.TimeZoneId,
            original.Schedules,
            original.Status,
            original.RemindersEnabled,
            original.Version,
            original.Revisions,
            original.CreatedAtUtc,
            original.UpdatedAtUtc);

        Assert.Equal(original.PlanId, restored.PlanId);
        Assert.Equal(original.Version, restored.Version);
        Assert.Empty(restored.PullDomainEvents());
    }

    private static FeedingPlan CreatePlan(
        IEnumerable<FeedingSchedule>? schedules = null)
    {
        return FeedingPlan.Prescribe(
            FeedingPlanId.New(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Veterinary food",
            "Follow the prescribed feeding plan.",
            StartDate,
            EndDate,
            "America/Lima",
            schedules ?? new[] { DailySchedule() },
            Now);
    }

    private static FeedingSchedule DailySchedule(
        TimeOnly? time = null)
    {
        return new FeedingSchedule(
            Guid.NewGuid(),
            time ?? new TimeOnly(8, 0),
            new FeedingPortion(150, "g"),
            Enum.GetValues<DayOfWeek>());
    }

    private static FeedingSchedule Schedule(
        TimeOnly time,
        params DayOfWeek[] days)
    {
        return new FeedingSchedule(
            Guid.NewGuid(),
            time,
            new FeedingPortion(150, "g"),
            days);
    }
}
