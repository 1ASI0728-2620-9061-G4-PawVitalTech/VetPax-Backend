
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Modules.AppointmentManagement.Repositories;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.AppointmentManagement;

public sealed class AppointmentInfrastructureTests
{
    private static async Task<VetPaxDbContext> OpenDbAsync()
    {
        var connection =
            Environment.GetEnvironmentVariable(
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
                "Integration tests only use vetpax_test_db.");
        }

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

    private static Appointment CreateAppointment(
        Guid veterinarianId,
        DateOnly date,
        TimeOnly startTime)
    {
        return Appointment.Schedule(
            new AppointmentOwnerId(Guid.NewGuid()),
            new AppointmentPetId(Guid.NewGuid()),
            new AppointmentClinicId(Guid.NewGuid()),
            new AppointmentVeterinarianId(veterinarianId),
            new AppointmentSlot(date, startTime),
            "Control veterinario general",
            DateTime.UtcNow);
    }

    [Fact]
    public async Task ScheduledAppointment_PersistsOutboxAndReminder()
    {
        await using var db = await OpenDbAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var date = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(7));

        var appointment = CreateAppointment(
            Guid.NewGuid(),
            date,
            new TimeOnly(9, 0));

        var repository = new AppointmentRepository(db);

        var notifications = new AppointmentNotificationAdapter(
            db,
            TimeProvider.System);

        await repository.SaveAsync(appointment);

        await notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Schedule,
            appointment.PullDomainEvents());

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        var saved = await repository.FindByIdAsync(
            appointment.AppointmentId);

        Assert.NotNull(saved);

        Assert.Equal(
            AppointmentStatus.Pending,
            saved.Status);

        var outbox = await db.AppointmentOutbox
            .AsNoTracking()
            .Where(x =>
                x.AppointmentId ==
                appointment.AppointmentId.Value)
            .ToListAsync();

        Assert.Single(outbox);

        Assert.Null(outbox[0].PublishedAtUtc);

        var reminder = await db.AppointmentReminders
            .AsNoTracking()
            .SingleAsync(
                x => x.AppointmentId ==
                     appointment.AppointmentId.Value);

        Assert.Equal(
            AppointmentReminderStatus.Pending,
            reminder.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task CancelledAppointment_ReleasesSlotAndReminder()
    {
        await using var db = await OpenDbAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var veterinarianId = Guid.NewGuid();

        var date = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(7));

        var slot = new AppointmentSlot(
            date,
            new TimeOnly(10, 0));

        var appointment = CreateAppointment(
            veterinarianId,
            date,
            slot.StartTime);

        var repository = new AppointmentRepository(db);

        var notifications = new AppointmentNotificationAdapter(
            db,
            TimeProvider.System);

        await repository.SaveAsync(appointment);

        await notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Schedule,
            appointment.PullDomainEvents());

        await db.SaveChangesAsync();

        Assert.True(await repository.HasOverlapAsync(
            appointment.VeterinarianId,
            slot));

        appointment.Cancel(DateTime.UtcNow);

        await repository.SaveAsync(appointment);

        await notifications.StageChangesAsync(
            appointment,
            AppointmentNotificationAction.Cancel,
            appointment.PullDomainEvents());

        await db.SaveChangesAsync();

        Assert.False(await repository.HasOverlapAsync(
            appointment.VeterinarianId,
            slot));

        var reminder = await db.AppointmentReminders
            .SingleAsync(x =>
                x.AppointmentId ==
                appointment.AppointmentId.Value);

        Assert.Equal(
            AppointmentReminderStatus.Cancelled,
            reminder.Status);

        var eventsCount = await db.AppointmentOutbox
            .CountAsync(x =>
                x.AppointmentId ==
                appointment.AppointmentId.Value);

        Assert.Equal(2, eventsCount);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task UniqueIndex_RejectsDoubleBooking()
    {
        await using var db = await OpenDbAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var veterinarianId = Guid.NewGuid();

        var date = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(7));

        var first = CreateAppointment(
            veterinarianId,
            date,
            new TimeOnly(11, 0));

        var second = CreateAppointment(
            veterinarianId,
            date,
            new TimeOnly(11, 0));

        var repository = new AppointmentRepository(db);

        await repository.SaveAsync(first);

        await db.SaveChangesAsync();

        await repository.SaveAsync(second);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        await transaction.RollbackAsync();
    }
}
