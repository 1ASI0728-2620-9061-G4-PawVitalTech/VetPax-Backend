
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Modules.MedicationTreatment.Repositories;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.MedicationTreatment;

public sealed class MedicationPersistenceTests
{
    private static async Task<VetPaxDbContext> OpenDbAsync()
    {
        var connection = Environment.GetEnvironmentVariable(
            "VETPAX_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "Configure VETPAX_TEST_MYSQL.");

        var builder = new MySqlConnectionStringBuilder(connection);

        if (!string.Equals(
                builder.Database,
                "vetpax_test_db",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Tests must use vetpax_test_db.");
        }

        var options = new DbContextOptionsBuilder<VetPaxDbContext>()
            .UseMySql(
                connection,
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        var db = new VetPaxDbContext(options);

        await db.Database.MigrateAsync();

        return db;
    }

    private static MedicationPlan CreatePlan()
    {
        var schedule = new MedicationSchedule(
            MedicationScheduleId.New(),
            new TimeOnly(8, 0),
            Enum.GetValues<DayOfWeek>(),
            new MedicationDosage(1m, "tablet"));

        return MedicationPlan.Prescribe(
            new MedicationOwnerId(Guid.NewGuid()),
            new MedicationPetId(Guid.NewGuid()),
            new MedicationClinicId(Guid.NewGuid()),
            new MedicationVeterinarianId(Guid.NewGuid()),
            "Test medication",
            "Oral",
            "Test prescription",
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 9),
            "America/Lima",
            new[] { schedule },
            new DateTime(
                2026, 10, 23, 12, 0, 0,
                DateTimeKind.Utc));
    }

    [Fact]
    public async Task Repository_SavesAndRestoresMedicationPlan()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var plan = CreatePlan();
        var repository = new MedicationPlanRepository(db);

        await repository.SaveAsync(plan);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var restored = await repository.FindByIdAsync(
            plan.PlanId);

        Assert.NotNull(restored);

        Assert.Equal(
            plan.PlanId,
            restored!.PlanId);

        Assert.Equal(
            plan.OwnerId,
            restored.OwnerId);

        Assert.Equal(
            MedicationPlanStatus.Active,
            restored.Status);

        Assert.Single(restored.Schedules);
        Assert.Empty(restored.PullDomainEvents());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Database_RejectsDuplicateAdministration()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var plan = CreatePlan();
        var repository = new MedicationPlanRepository(db);

        var scheduledAt = new DateTime(
            2026, 11, 2, 13, 0, 0,
            DateTimeKind.Utc);

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            new DateOnly(2026, 11, 2),
            scheduledAt,
            scheduledAt.AddDays(-1));

        await repository.SaveAsync(plan);
        await db.SaveChangesAsync();

        var first = new MedicationAdministrationRow
        {
            AdministrationId = Guid.NewGuid(),
            DoseId = dose.DoseId.Value,
            AdministeredBy = plan.OwnerId.Value,
            AdministeredAtUtc = scheduledAt,
            RecordedAtUtc = scheduledAt
        };

        db.Set<MedicationAdministrationRow>().Add(first);
        await db.SaveChangesAsync();

        var duplicate = new MedicationAdministrationRow
        {
            AdministrationId = Guid.NewGuid(),
            DoseId = dose.DoseId.Value,
            AdministeredBy = plan.OwnerId.Value,
            AdministeredAtUtc = scheduledAt,
            RecordedAtUtc = scheduledAt
        };

        db.Set<MedicationAdministrationRow>()
            .Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Outbox_IsSavedWithMedicationPlan()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var plan = CreatePlan();
        var repository = new MedicationPlanRepository(db);
        var outbox = new MedicationOutboxAdapter(db);

        await repository.SaveAsync(plan);

        var events = plan.PullDomainEvents().ToArray();

        await outbox.StageAsync(events);

        // Un solo SaveChanges para plan + evento.
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var savedPlan = await db.Set<MedicationPlanRow>()
            .AsNoTracking()
            .SingleAsync(x =>
                x.PlanId == plan.PlanId.Value);

        var savedEvent = await db.Set<MedicationOutboxRow>()
            .AsNoTracking()
            .SingleAsync(x =>
                x.PlanId == plan.PlanId.Value);

        Assert.Equal(
            plan.PlanId.Value,
            savedPlan.PlanId);

        Assert.Null(savedEvent.PublishedAtUtc);
        Assert.Equal(0, savedEvent.Attempts);

        Assert.Contains(
            "MedicationPlanPrescribedEvent",
            savedEvent.EventType);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Reminders_AreCancelledWithoutDeletingHistory()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var plan = CreatePlan();

        plan.ConfigureReminders(
            true,
            new DateTime(
                2026, 10, 23, 12, 1, 0,
                DateTimeKind.Utc));

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            new DateOnly(2026, 11, 2),
            new DateTime(
                2026, 11, 2, 13, 0, 0,
                DateTimeKind.Utc),
            new DateTime(
                2026, 10, 23, 12, 2, 0,
                DateTimeKind.Utc));

        var repository = new MedicationPlanRepository(db);
        var reminders = new MedicationReminderAdapter(db);

        await repository.SaveAsync(plan);
        await reminders.StageForDoseAsync(plan, dose);

        await db.SaveChangesAsync();

        await reminders.CancelForDoseAsync(
            dose.DoseId.Value);

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await db.Set<MedicationReminderRow>()
            .AsNoTracking()
            .SingleAsync(x =>
                x.DoseId == dose.DoseId.Value);

        Assert.Equal(
            MedicationReminderDeliveryStatus.Cancelled,
            saved.Status);

        Assert.Null(saved.SentAtUtc);

        await transaction.RollbackAsync();
    }
}
