
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Modules.MedicationTreatment.Repositories;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.MedicationTreatment;

public sealed class MedicationConcurrencyTests
{
    private static async Task<VetPaxDbContext> OpenAsync()
    {
        var connection = Environment.GetEnvironmentVariable(
            "VETPAX_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "Configure VETPAX_TEST_MYSQL.");

        if (!string.Equals(
                new MySqlConnectionStringBuilder(connection).Database,
                "vetpax_test_db",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only vetpax_test_db is allowed.");
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

    [Fact]
    public async Task ConcurrentUpdates_SecondWriterIsRejected()
    {
        var plan = MedicationPlan.Prescribe(
            new MedicationOwnerId(Guid.NewGuid()),
            new MedicationPetId(Guid.NewGuid()),
            new MedicationClinicId(Guid.NewGuid()),
            new MedicationVeterinarianId(Guid.NewGuid()),
            "Medication concurrency test",
            "Oral",
            "",
            new DateOnly(2026, 11, 2),
            new DateOnly(2026, 11, 9),
            "UTC",
            new[]
            {
                new MedicationSchedule(
                    MedicationScheduleId.New(),
                    new TimeOnly(8, 0),
                    Enum.GetValues<DayOfWeek>(),
                    new MedicationDosage(1m, "tablet"))
            },
            new DateTime(
                2026, 10, 23, 12, 0, 0,
                DateTimeKind.Utc));

        var planId = plan.PlanId;

        await using (var seedDb = await OpenAsync())
        {
            var repository = new MedicationPlanRepository(seedDb);
            await repository.SaveAsync(plan);
            await seedDb.SaveChangesAsync();
        }

        try
        {
            await using var dbA = await OpenAsync();
            await using var dbB = await OpenAsync();

            var repositoryA = new MedicationPlanRepository(dbA);
            var repositoryB = new MedicationPlanRepository(dbB);

            // Ambos contextos leen la misma versión.
            var first = await repositoryA.FindByIdAsync(planId);
            var second = await repositoryB.FindByIdAsync(planId);

            Assert.NotNull(first);
            Assert.NotNull(second);

            var changedAt = new DateTime(
                2026, 10, 23, 12, 5, 0,
                DateTimeKind.Utc);

            first!.ConfigureReminders(true, changedAt);
            second!.ConfigureReminders(true, changedAt);

            // Primera actualización: Version 1 -> 2.
            await repositoryA.SaveAsync(first);
            await dbA.SaveChangesAsync();

            // Segunda actualización con Version 1.
            // MySQL ya tiene Version 2.
            await repositoryB.SaveAsync(second);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => dbB.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = await OpenAsync();

            await cleanup.Set<MedicationReminderRow>()
                .Where(x => x.PlanId == planId.Value)
                .ExecuteDeleteAsync();

            await cleanup.Set<MedicationReminderPreferenceRow>()
                .Where(x => x.PlanId == planId.Value)
                .ExecuteDeleteAsync();

            await cleanup.Set<MedicationOutboxRow>()
                .Where(x => x.PlanId == planId.Value)
                .ExecuteDeleteAsync();

            await cleanup.Set<MedicationAdministrationRow>()
                .Where(x =>
                    cleanup.Set<MedicationDoseRow>()
                        .Any(d =>
                            d.DoseId == x.DoseId &&
                            d.PlanId == planId.Value))
                .ExecuteDeleteAsync();

            await cleanup.Set<MedicationDoseRow>()
                .Where(x => x.PlanId == planId.Value)
                .ExecuteDeleteAsync();

            await cleanup.Set<MedicationScheduleRow>()
                .Where(x => x.PlanId == planId.Value)
                .ExecuteDeleteAsync();

            await cleanup.Set<MedicationPlanRow>()
                .Where(x => x.PlanId == planId.Value)
                .ExecuteDeleteAsync();
        }
    }
}
