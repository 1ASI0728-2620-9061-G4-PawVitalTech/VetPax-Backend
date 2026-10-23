
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.ClinicManagement.Persistence;
using VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;
using VetPax.Infrastructure.Modules.PetClinicalCare.Persistence;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.PetClinicalCare;

public sealed class VeterinaryAccessMySqlTests
{
    private static async Task<VetPaxDbContext> CreateDbAsync()
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
    public async Task AuthorizedVeterinarian_CanAccessLinkedPatient()
    {
        await using var db = await CreateDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var clinicId = Guid.NewGuid();
        var veterinarianId = Guid.NewGuid();
        var petId = Guid.NewGuid();

        db.Clinics.Add(new ClinicRow
        {
            ClinicId = clinicId,
            Name = "Clinica de prueba",
            Address = "Direccion de prueba",
            UpdatedAt = DateTime.UtcNow
        });

        db.ClinicVeterinarians.Add(
            new ClinicVeterinarianRow
            {
                ClinicId = clinicId,
                VeterinarianId = veterinarianId,
                IsActive = true
            });

        db.PetClinicLinks.Add(new PetClinicLinkRow
        {
            ClinicId = clinicId,
            PetId = petId,
            IsActive = true,
            LinkedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var authorization = new VerifiedClinicalAuthorizationAdapter(
            new ClinicalRelationshipAdapter(db),
            new VeterinaryClinicalAccessPolicy());

        var requester = new AccessRequester(
            veterinarianId,
            RequesterRole.Veterinarian,
            new ClinicId(clinicId));

        Assert.True(await authorization.IsAuthorizedAsync(
            new PetId(petId),
            requester));

        var membership = await db.ClinicVeterinarians
            .SingleAsync(x =>
                x.ClinicId == clinicId &&
                x.VeterinarianId == veterinarianId);

        membership.IsActive = false;
        await db.SaveChangesAsync();

        Assert.False(await authorization.IsAuthorizedAsync(
            new PetId(petId),
            requester));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task UnlinkedPatient_IsDenied()
    {
        await using var db = await CreateDbAsync();
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var clinicId = Guid.NewGuid();
        var veterinarianId = Guid.NewGuid();

        db.Clinics.Add(new ClinicRow
        {
            ClinicId = clinicId,
            Name = "Clinica sin pacientes",
            Address = "Direccion de prueba",
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

        var authorization = new VerifiedClinicalAuthorizationAdapter(
            new ClinicalRelationshipAdapter(db),
            new VeterinaryClinicalAccessPolicy());

        var requester = new AccessRequester(
            veterinarianId,
            RequesterRole.Veterinarian,
            new ClinicId(clinicId));

        var result = await authorization.IsAuthorizedAsync(
            PetId.New(),
            requester);

        Assert.False(result);

        await transaction.RollbackAsync();
    }
}
