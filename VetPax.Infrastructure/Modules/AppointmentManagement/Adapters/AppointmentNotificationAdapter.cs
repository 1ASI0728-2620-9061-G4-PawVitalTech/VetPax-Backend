
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;

public sealed class AppointmentNotificationAdapter
    : IAppointmentNotificationPort
{
    private static readonly TimeSpan PeruOffset =
        TimeSpan.FromHours(-5);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly VetPaxDbContext _db;
    private readonly TimeProvider _clock;

    public AppointmentNotificationAdapter(
        VetPaxDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task StageChangesAsync(
        Appointment appointment,
        AppointmentNotificationAction action,
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        ArgumentNullException.ThrowIfNull(domainEvents);

        var now = _clock.GetUtcNow().UtcDateTime;

        foreach (var domainEvent in domainEvents)
        {
            _db.AppointmentOutbox.Add(
                new AppointmentOutboxRow
                {
                    EventId = Guid.NewGuid(),
                    AppointmentId =
                        appointment.AppointmentId.Value,
                    EventType =
                        domainEvent.GetType().FullName
                        ?? domainEvent.GetType().Name,
                    Payload = JsonSerializer.Serialize(
                        domainEvent,
                        domainEvent.GetType(),
                        JsonOptions),
                    OccurredAtUtc = now,
                    NextAttemptAtUtc = now,
                    PublishedAtUtc = null,
                    Attempts = 0,
                    LastError = null
                });
        }

        var reminder = await _db.AppointmentReminders
            .SingleOrDefaultAsync(
                x => x.AppointmentId ==
                     appointment.AppointmentId.Value,
                cancellationToken);

        switch (action)
        {
            case AppointmentNotificationAction.Schedule:
            case AppointmentNotificationAction.Reschedule:
                StageReminder(
                    appointment,
                    reminder,
                    now);
                break;

            case AppointmentNotificationAction.Cancel:
            case AppointmentNotificationAction.Attended:
                CancelReminder(reminder, now);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(action),
                    action,
                    "Unknown appointment notification action.");
        }

        // Deliberadamente no llamamos a SaveChangesAsync.
        // Todas las escrituras serán confirmadas por IUnitOfWork.
    }

    private void StageReminder(
        Appointment appointment,
        AppointmentReminderRow? existing,
        DateTime now)
    {
        var localDateTime = appointment.Slot.Date
            .ToDateTime(appointment.Slot.StartTime);

        var startsAtUtc = new DateTimeOffset(
            localDateTime,
            PeruOffset).UtcDateTime;

        var reminderDue = startsAtUtc.AddHours(-24);

        if (reminderDue < now)
            reminderDue = now;

        if (existing is null)
        {
            _db.AppointmentReminders.Add(
                new AppointmentReminderRow
                {
                    AppointmentId =
                        appointment.AppointmentId.Value,
                    OwnerId = appointment.OwnerId.Value,
                    DueAtUtc = reminderDue,
                    Status = AppointmentReminderStatus.Pending,
                    UpdatedAtUtc = now,
                    SentAtUtc = null
                });

            return;
        }

        existing.OwnerId = appointment.OwnerId.Value;
        existing.DueAtUtc = reminderDue;
        existing.Status = AppointmentReminderStatus.Pending;
        existing.UpdatedAtUtc = now;
        existing.SentAtUtc = null;
    }

    private static void CancelReminder(
        AppointmentReminderRow? reminder,
        DateTime now)
    {
        if (reminder is null)
            return;

        // Conservamos evidencia de recordatorios ya enviados.
        if (reminder.Status == AppointmentReminderStatus.Sent)
            return;

        reminder.Status = AppointmentReminderStatus.Cancelled;
        reminder.UpdatedAtUtc = now;
    }
}
