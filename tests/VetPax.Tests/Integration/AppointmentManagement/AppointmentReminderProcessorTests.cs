
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Modules.AppointmentManagement.Processing;
using VetPax.Infrastructure.Persistence;
using Xunit;

namespace VetPax.Tests.Integration.AppointmentManagement;

public sealed class AppointmentReminderProcessorTests
{
    private sealed class FakeReminderSender
        : IAppointmentReminderSender
    {
        public int SentCount { get; private set; }

        public Task SendAsync(
            AppointmentRow appointment,
            CancellationToken cancellationToken = default)
        {
            SentCount++;
            return Task.CompletedTask;
        }
    }

    private static async Task<VetPaxDbContext> CreateDatabaseAsync()
    {
        var connection = Environment.GetEnvironmentVariable(
            "VETPAX_TEST_MYSQL")
            ?? throw new InvalidOperationException(
                "VETPAX_TEST_MYSQL is required.");

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

        return new VetPaxDbContext(options);
    }

    private static async Task<Guid> SeedAppointmentAsync(
        VetPaxDbContext db,
        AppointmentReminderStatus reminderStatus)
    {
        var appointmentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Appointments.Add(new AppointmentRow
        {
            AppointmentId = appointmentId,
            OwnerId = Guid.NewGuid(),
            PetId = Guid.NewGuid(),
            ClinicId = Guid.NewGuid(),
            VeterinarianId = Guid.NewGuid(),
            AppointmentDate = DateOnly.FromDateTime(
                now.AddDays(1)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30),
            Reason = "Control general",
            Status = AppointmentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        });

        db.AppointmentReminders.Add(
            new AppointmentReminderRow
            {
                AppointmentId = appointmentId,
                OwnerId = Guid.NewGuid(),
                DueAtUtc = now.AddMinutes(-5),
                Status = reminderStatus,
                UpdatedAtUtc = now.AddMinutes(-10),
                SentAtUtc = null
            });

        await db.SaveChangesAsync();

        return appointmentId;
    }

    [Fact]
    public async Task DueReminder_IsSentOnlyOnce()
    {
        await using var db = await CreateDatabaseAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var appointmentId = await SeedAppointmentAsync(
            db,
            AppointmentReminderStatus.Pending);

        var sender = new FakeReminderSender();

        var processor = new AppointmentReminderProcessor(
            db,
            sender,
            TimeProvider.System,
            NullLogger<AppointmentReminderProcessor>.Instance);

        var first = await processor.ProcessBatchAsync();
        var second = await processor.ProcessBatchAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, sender.SentCount);

        db.ChangeTracker.Clear();

        var reminder = await db.AppointmentReminders
            .AsNoTracking()
            .SingleAsync(x =>
                x.AppointmentId == appointmentId);

        Assert.Equal(
            AppointmentReminderStatus.Sent,
            reminder.Status);

        Assert.NotNull(reminder.SentAtUtc);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task CancelledReminder_IsNotSent()
    {
        await using var db = await CreateDatabaseAsync();

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        await SeedAppointmentAsync(
            db,
            AppointmentReminderStatus.Cancelled);

        var sender = new FakeReminderSender();

        var processor = new AppointmentReminderProcessor(
            db,
            sender,
            TimeProvider.System,
            NullLogger<AppointmentReminderProcessor>.Instance);

        var sent = await processor.ProcessBatchAsync();

        Assert.Equal(0, sent);
        Assert.Equal(0, sender.SentCount);

        await transaction.RollbackAsync();
    }
}
