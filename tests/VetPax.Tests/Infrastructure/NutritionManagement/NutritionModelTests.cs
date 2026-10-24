
using Microsoft.EntityFrameworkCore;
using VetPax.Infrastructure.Modules.NutritionManagement.Persistence;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Infrastructure.NutritionManagement;

public sealed class NutritionModelTests
{
    private static VetPaxDbContext CreateContext()
    {
        // This test inspects EF metadata only.
        // It does not connect to MySQL.
        var options = new DbContextOptionsBuilder<VetPaxDbContext>()
            .UseMySql(
                "Server=localhost;Database=nutrition_model_test;" +
                "User ID=model_only;Password=model_only;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new VetPaxDbContext(options);
    }

    [Fact]
    public void Model_ContainsAllNutritionTables()
    {
        using var db = CreateContext();

        AssertTable<FeedingPlanRow>(
            db, "nutrition_feeding_plans");

        AssertTable<FeedingActivePlanRow>(
            db, "nutrition_active_plans");

        AssertTable<FeedingScheduleRow>(
            db, "nutrition_feeding_schedules");

        AssertTable<FeedingPlanRevisionRow>(
            db, "nutrition_plan_revisions");

        AssertTable<FeedingReminderRow>(
            db, "nutrition_reminders");

        AssertTable<FeedingOutboxRow>(
            db, "nutrition_outbox");
    }

    [Fact]
    public void ActivePlan_UsesUniquePetId()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(FeedingActivePlanRow));

        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties);

        Assert.Equal(
            nameof(FeedingActivePlanRow.PetId),
            primaryKey.Properties[0].Name);
    }

    [Fact]
    public void Reminder_HasUniqueOccurrenceIndex()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(FeedingReminderRow));

        Assert.NotNull(entity);

        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[]
                {
                    nameof(FeedingReminderRow.PlanId),
                    nameof(FeedingReminderRow.PlanVersion),
                    nameof(FeedingReminderRow.ScheduleId),
                    nameof(FeedingReminderRow.LocalDate)
                }));
    }

    [Fact]
    public void FeedingPlan_HasConcurrencyToken()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(FeedingPlanRow));

        Assert.NotNull(entity);

        var property = entity.FindProperty(
            nameof(FeedingPlanRow.ConcurrencyStamp));

        Assert.NotNull(property);
        Assert.True(property.IsConcurrencyToken);
    }

    private static void AssertTable<TEntity>(
        VetPaxDbContext db,
        string expectedTable)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity));

        Assert.NotNull(entity);
        Assert.Equal(expectedTable, entity.GetTableName());
    }
}
