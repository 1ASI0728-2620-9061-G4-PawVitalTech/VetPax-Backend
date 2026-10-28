
using Microsoft.EntityFrameworkCore;
using VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Infrastructure.AdherenceGamification;

public sealed class AdherenceModelTests
{
    [Fact]
    public void Model_RegistersAllAdherenceTables()
    {
        using var db = CreateContext();

        AssertTable<AdherenceRow>(
            db, "adherence_profiles");

        AssertTable<ProcessedAdherenceEventRow>(
            db, "adherence_processed_events");

        AssertTable<AdherenceRecognitionRow>(
            db, "adherence_recognitions");

        AssertTable<AdherenceOutboxRow>(
            db, "adherence_outbox");
    }

    [Fact]
    public void Evidence_HasUniqueSourceIndex()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(ProcessedAdherenceEventRow));

        Assert.NotNull(entity);

        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[]
                {
                    nameof(ProcessedAdherenceEventRow.Kind),
                    nameof(ProcessedAdherenceEventRow.SourceId)
                }));
    }

    [Fact]
    public void EvidenceEventId_IsPrimaryKey()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(ProcessedAdherenceEventRow));

        Assert.NotNull(entity);

        var key = entity.FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Single(key.Properties);

        Assert.Equal(
            nameof(ProcessedAdherenceEventRow.EventId),
            key.Properties[0].Name);
    }

    [Fact]
    public void Profile_UsesConcurrencyToken()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(AdherenceRow));

        Assert.NotNull(entity);

        var property = entity.FindProperty(
            nameof(AdherenceRow.ConcurrencyStamp));

        Assert.NotNull(property);
        Assert.True(property.IsConcurrencyToken);
    }

    [Fact]
    public void Recognition_HasForeignKeyToProfile()
    {
        using var db = CreateContext();

        var entity = db.Model.FindEntityType(
            typeof(AdherenceRecognitionRow));

        Assert.NotNull(entity);

        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType ==
                typeof(AdherenceRow) &&
            fk.Properties.Any(p =>
                p.Name == nameof(AdherenceRecognitionRow.OwnerId)));
    }

    private static void AssertTable<TEntity>(
        VetPaxDbContext db,
        string tableName)
    {
        var entity = db.Model.FindEntityType(
            typeof(TEntity));

        Assert.NotNull(entity);
        Assert.Equal(tableName, entity.GetTableName());
    }

    private static VetPaxDbContext CreateContext()
    {
        // Only inspect the EF model.
        // No connection to MySQL is opened.
        var options = new DbContextOptionsBuilder<VetPaxDbContext>()
            .UseMySql(
                "Server=localhost;Database=adherence_model_only;" +
                "User ID=test;Password=test;",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new VetPaxDbContext(options);
    }
}
