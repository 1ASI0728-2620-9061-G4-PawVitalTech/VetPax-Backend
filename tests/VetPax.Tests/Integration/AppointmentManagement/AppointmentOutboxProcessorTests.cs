
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using NSubstitute;
using VetPax.Application.Common.Ports;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Modules.AppointmentManagement.Processing;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.AppointmentManagement;

public sealed class AppointmentOutboxProcessorTests
{
    [Fact]
    public async Task InvalidEvent_RemainsPendingWithError()
    {
        // ---------------------------------------------
        // ARRANGE: conexión exclusiva a base de pruebas
        // ---------------------------------------------

        var connection = Environment.GetEnvironmentVariable(
            "VETPAX_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "VETPAX_TEST_MYSQL is required.");

        var connectionBuilder =
            new MySqlConnectionStringBuilder(connection);

        if (!string.Equals(
                connectionBuilder.Database,
                "vetpax_test_db",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Tests must use vetpax_test_db.");
        }

        var options =
            new DbContextOptionsBuilder<VetPaxDbContext>()
                .UseMySql(
                    connection,
                    new MySqlServerVersion(
                        new Version(8, 0, 0)))
                .Options;

        await using var db = new VetPaxDbContext(options);

        // Todos los cambios realizados por el procesador
        // quedarán dentro de esta transacción de pruebas.
        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var eventId = Guid.NewGuid();

        var now = DateTime.UtcNow;

        // Una fecha antigua permite que nuestro evento
        // tenga prioridad sobre los eventos pendientes
        // generados por otros escenarios BDD.
        var occurredAt = new DateTime(
            2000, 1, 1, 0, 0, 0,
            DateTimeKind.Utc);

        db.AppointmentOutbox.Add(
            new AppointmentOutboxRow
            {
                EventId = eventId,
                AppointmentId = Guid.NewGuid(),
                EventType = "UnsupportedEventForTest",
                Payload = "{}",
                OccurredAtUtc = occurredAt,
                NextAttemptAtUtc = now.AddMinutes(-5),
                PublishedAtUtc = null,
                Attempts = 0,
                LastError = null
            });

        await db.SaveChangesAsync();

        // Publicador simulado: no publica eventos reales.
        var publisher =
            Substitute.For<IDomainEventPublisher>();

        var processor = new AppointmentOutboxProcessor(
            db,
            publisher,
            TimeProvider.System,
            NullLogger<AppointmentOutboxProcessor>.Instance);

        // ---------------------------------------------
        // ACT: procesar los eventos pendientes
        // ---------------------------------------------

        await processor.ProcessBatchAsync();

        // No comprobamos el número global de eventos
        // publicados, porque la base de pruebas puede
        // contener eventos anteriores de otros escenarios.

        db.ChangeTracker.Clear();

        // ---------------------------------------------
        // ASSERT: verificar NUESTRO evento específico
        // ---------------------------------------------

        var saved = await db.AppointmentOutbox
            .AsNoTracking()
            .SingleAsync(x => x.EventId == eventId);

        // El evento inválido no debe marcarse
        // como publicado.
        Assert.Null(saved.PublishedAtUtc);

        // El procesador debe haber intentado procesarlo.
        Assert.Equal(1, saved.Attempts);

        // Debe almacenar el mensaje de error.
        Assert.False(
            string.IsNullOrWhiteSpace(saved.LastError));

        Assert.Contains(
            "Unsupported appointment event",
            saved.LastError!);

        // Debe programar un reintento futuro.
        Assert.True(
            saved.NextAttemptAtUtc > now,
            "The failed event must be scheduled for retry.");

        // ---------------------------------------------
        // CLEANUP: deshacer todos los cambios
        // ---------------------------------------------

        await transaction.RollbackAsync();
    }
}
