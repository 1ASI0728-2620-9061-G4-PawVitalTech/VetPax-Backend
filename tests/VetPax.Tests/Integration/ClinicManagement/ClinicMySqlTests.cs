
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Infrastructure.Modules.ClinicManagement.Adapters;
using VetPax.Infrastructure.Modules.ClinicManagement.Persistence;
using VetPax.Infrastructure.Modules.ClinicManagement.Repositories;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.ClinicManagement;

public sealed class ClinicMySqlTests
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
            database,
            "vetpax_test_db",
            StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Only vetpax_test_db is allowed.");

        var options =
            new DbContextOptionsBuilder<VetPaxDbContext>()
                .UseMySql(
                    connection,
                    new MySqlServerVersion(
                        new Version(8, 0, 0)))
                .Options;

        var db = new VetPaxDbContext(options);
        await db.Database.MigrateAsync();

        return db;
    }

    [Fact]
    public async Task US12_ShouldPersistClinicAndSchedule()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var adminId = Guid.NewGuid();
        var clinicId = ClinicId.New();

        var clinic = new Clinic(
            clinicId,
            new AdministratorId(adminId),
            new ClinicGeneralData(
                "VetPax",
                "Av. Central 123"),
            new OpeningSchedule(new[]
            {
                new DailyOpeningHours(
                    DayOfWeek.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0))
            }));

        var repository = new ClinicRepository(db);

        await repository.SaveAsync(clinic);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var loaded = await repository.FindByIdAsync(
            clinicId);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsManagedBy(adminId));
        Assert.Single(loaded.OpeningSchedule.Days);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task US10_ShouldValidateActiveVeterinarianMembership()
    {
        await using var db = await OpenDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var clinicId = Guid.NewGuid();
        var veterinarianId = Guid.NewGuid();

        db.Clinics.Add(new ClinicRow
        {
            ClinicId = clinicId,
            Name = "VetPax",
            Address = "Av. Central 123",
            UpdatedAt = DateTime.UtcNow
        });

        db.ClinicVeterinarians.Add(
            new ClinicVeterinarianRow
            {
                ClinicId = clinicId,
                VeterinarianId = veterinarianId,
                IsActive = true
            });

        await db.SaveChangesAsync();

        var membership = new ClinicMembershipAdapter(db);

        Assert.True(
            await membership.IsActiveVeterinarianAsync(
                veterinarianId,
                new ClinicId(clinicId)));

        Assert.False(
            await membership.IsActiveVeterinarianAsync(
                Guid.NewGuid(),
                new ClinicId(clinicId)));

        await transaction.RollbackAsync();
    }
}
