
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Infrastructure.MedicationTreatment;

public sealed class MedicationDeviceTokenIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterDevice_PersistsActiveToken()
    {
        await RunWithFreshTokenAsync(async (db, store, token) =>
        {
            var ownerId = Guid.NewGuid();

            var deviceId = await store.RegisterAsync(
                ownerId, token, "android");

            // Consultar desde otro DbContext para comprobar
            // que el dato realmente se guardó.
            await using var verificationDb =
                CreateContext();

            var persisted = await verificationDb
                .Set<MedicationDeviceTokenRow>()
                .AsNoTracking()
                .SingleAsync(x => x.DeviceId == deviceId);

            Assert.Equal(ownerId, persisted.OwnerId);
            Assert.Equal(token, persisted.Token);
            Assert.Equal("android", persisted.Platform);
            Assert.True(persisted.IsActive);

            var active = await store.GetActiveTokensAsync(ownerId);

            Assert.Contains(active, x => x.DeviceId == deviceId);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task RegisterSameToken_ReusesDeviceAndUpdatesOwner()
    {
        await RunWithFreshTokenAsync(async (db, store, token) =>
        {
            var firstOwner = Guid.NewGuid();
            var secondOwner = Guid.NewGuid();

            var firstDeviceId = await store.RegisterAsync(
                firstOwner, token, "android");

            var secondDeviceId = await store.RegisterAsync(
                secondOwner, token, "ios");

            Assert.Equal(firstDeviceId, secondDeviceId);

            var oldOwnerDevices =
                await store.GetActiveTokensAsync(firstOwner);

            Assert.DoesNotContain(
                oldOwnerDevices,
                x => x.DeviceId == firstDeviceId);

            var newOwnerDevices =
                await store.GetActiveTokensAsync(secondOwner);

            var device = Assert.Single(
                newOwnerDevices,
                x => x.DeviceId == secondDeviceId);

            Assert.Equal(secondOwner, device.OwnerId);
            Assert.Equal("ios", device.Platform);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OtherOwner_CannotDisableDevice()
    {
        await RunWithFreshTokenAsync(async (db, store, token) =>
        {
            var ownerId = Guid.NewGuid();
            var otherOwnerId = Guid.NewGuid();

            var deviceId = await store.RegisterAsync(
                ownerId, token, "android");

            var disabled = await store.DisableAsync(
                otherOwnerId, deviceId);

            Assert.False(disabled);

            var devices =
                await store.GetActiveTokensAsync(ownerId);

            Assert.Contains(
                devices,
                x => x.DeviceId == deviceId && x.IsActive);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DisableDevice_RemovesItFromActiveDevices()
    {
        await RunWithFreshTokenAsync(async (db, store, token) =>
        {
            var ownerId = Guid.NewGuid();

            var deviceId = await store.RegisterAsync(
                ownerId, token, "web");

            var disabled = await store.DisableAsync(
                ownerId, deviceId);

            Assert.True(disabled);

            var devices =
                await store.GetActiveTokensAsync(ownerId);

            Assert.DoesNotContain(
                devices,
                x => x.DeviceId == deviceId);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExpiredToken_IsNotReturnedAsActive()
    {
        await RunWithFreshTokenAsync(async (db, store, token) =>
        {
            var ownerId = Guid.NewGuid();

            var deviceId = await store.RegisterAsync(
                ownerId, token, "android");

            // Simular un dispositivo sin actualizar
            // su token durante más de 30 días.
            var oldDate = DateTime.UtcNow.AddDays(-31);

            await db.Set<MedicationDeviceTokenRow>()
                .Where(x => x.DeviceId == deviceId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(
                        x => x.LastSeenAtUtc,
                        oldDate));

            var devices =
                await store.GetActiveTokensAsync(ownerId);

            Assert.DoesNotContain(
                devices,
                x => x.DeviceId == deviceId);
        });
    }

    // -------------------------------------------
    // Test infrastructure
    // -------------------------------------------

    private static async Task RunWithFreshTokenAsync(
        Func<
            VetPaxDbContext,
            MedicationDeviceTokenStore,
            string,
            Task> scenario)
    {
        var token = $"vetpax-test-{Guid.NewGuid():N}";
        var tokenHash = MedicationDeviceTokenStore.HashToken(token);

        await using (var db = CreateContext())
        {
            try
            {
                var store = new MedicationDeviceTokenStore(
                    db,
                    TimeProvider.System);

                await scenario(db, store, token);
            }
            finally
            {
                // Se eliminan únicamente los registros
                // creados para esta prueba.
                await using var cleanupDb = CreateContext();

                var rows = await cleanupDb
                    .Set<MedicationDeviceTokenRow>()
                    .Where(x => x.TokenHash == tokenHash)
                    .ToListAsync();

                cleanupDb.Set<MedicationDeviceTokenRow>()
                    .RemoveRange(rows);

                await cleanupDb.SaveChangesAsync();
            }
        }
    }

    private static VetPaxDbContext CreateContext()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "VETPAX_TEST_MYSQL");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "VETPAX_TEST_MYSQL is required " +
                "to run MedicationTreatment integration tests.");
        }

        var connection = new MySqlConnectionStringBuilder(
            connectionString);

        // Protección: estas pruebas nunca deben
        // modificar la base de datos principal.
        if (!string.Equals(
                connection.Database,
                "vetpax_test_db",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Integration tests can only use vetpax_test_db.");
        }

        var options =
            new DbContextOptionsBuilder<VetPaxDbContext>()
                .UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString))
                .Options;

        return new VetPaxDbContext(options);
    }
}
