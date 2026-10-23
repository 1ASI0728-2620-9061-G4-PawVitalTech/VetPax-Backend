
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalHistory;
using VetPax.Domain.Modules.PetClinicalCare.Entities;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;
using VetPax.Infrastructure.Modules.PetClinicalCare.Repositories;

namespace VetPax.Tests.Integration.PetClinicalCare;

public sealed class PetClinicalCareMySqlTests
{
    private static async Task<VetPaxDbContext> OpenDbAsync()
    {
        var connection = Environment.GetEnvironmentVariable(
            "VETPAX_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "Configure VETPAX_TEST_MYSQL.");

        var database = new MySqlConnectionStringBuilder(
            connection).Database;

        if (!string.Equals(
                database, "vetpax_test_db",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only vetpax_test_db is allowed for tests.");
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

    private static Task<RegisterPetResult> RegisterAsync(
        VetPaxDbContext db,
        OwnerId ownerId)
    {
        var handler = new RegisterPetHandler(
            new PetRepository(db),
            new ClinicalRecordRepository(db),
            new UnitOfWork(db));

        return handler.HandleAsync(new RegisterPetCommand(
            ownerId,
            "Luna",
            Species.Dog,
            new DateOnly(2022, 5, 10)));
    }

    [Fact]
    public async Task US01_ShouldPersistPetAndClinicalRecord()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var owner = new OwnerId(Guid.NewGuid());
        var result = await RegisterAsync(db, owner);

        var pet = await db.Pets.AsNoTracking()
            .SingleAsync(p => p.PetId == result.PetId);

        var record = await db.ClinicalRecords.AsNoTracking()
            .SingleAsync(r =>
                r.ClinicalRecordId == result.ClinicalRecordId);

        Assert.Equal(owner, pet.OwnerId);
        Assert.Equal(pet.PetId, record.PetId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task US02_ShouldReturnEmptyHistoryAndDenyStranger()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var owner = new OwnerId(Guid.NewGuid());
        var registered = await RegisterAsync(db, owner);

        db.ChangeTracker.Clear();

        var handler = new GetClinicalHistoryHandler(
            new PetRepository(db),
            new ClinicalRecordRepository(db));

        var history = await handler.HandleAsync(
            new GetClinicalHistoryQuery(
                registered.PetId, owner));

        Assert.NotNull(history);
        Assert.Empty(history.Attentions);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(
                new GetClinicalHistoryQuery(
                    registered.PetId,
                    new OwnerId(Guid.NewGuid()))));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task US02_ShouldReturnChronologicalAttentions()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var owner = new OwnerId(Guid.NewGuid());
        var registered = await RegisterAsync(db, owner);

        db.ChangeTracker.Clear();

        var repository = new ClinicalRecordRepository(db);

        var record = await repository.FindByPetIdAsync(
            registered.PetId);

        Assert.NotNull(record);

        var clinic = new ClinicId(Guid.NewGuid());
        var vet = new VeterinarianId(Guid.NewGuid());

        foreach (var daysAgo in new[] { 1, 7 })
        {
            record.RegisterAttention(new ClinicalAttention(
                AttentionId.New(),
                clinic,
                vet,
                DateTime.UtcNow.AddDays(-daysAgo),
                "Checkup",
                "Healthy",
                "Monitoring",
                indicators: new[]
                {
                    new ClinicalIndicator("Weight", 10m, "kg")
                }));
        }

        await repository.SaveAsync(record);
        await new UnitOfWork(db).CommitAsync();

        // Force a fresh read from MySQL
        db.ChangeTracker.Clear();

        var handler = new GetClinicalHistoryHandler(
            new PetRepository(db),
            new ClinicalRecordRepository(db));

        var history = await handler.HandleAsync(
            new GetClinicalHistoryQuery(
                registered.PetId, owner));

        Assert.NotNull(history);
        Assert.Equal(2, history.Attentions.Count);
        Assert.True(
            history.Attentions[0].AttentionDate <
            history.Attentions[1].AttentionDate);
        Assert.Single(history.Attentions[0].Indicators);

        await transaction.RollbackAsync();
    }
}
