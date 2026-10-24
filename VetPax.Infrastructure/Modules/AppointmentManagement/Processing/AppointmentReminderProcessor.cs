
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

public sealed class AppointmentReminderProcessor
{
    private readonly VetPaxDbContext _db;
    private readonly IAppointmentReminderSender _sender;
    private readonly TimeProvider _clock;
    private readonly ILogger<AppointmentReminderProcessor> _logger;

    public AppointmentReminderProcessor(
        VetPaxDbContext db,
        IAppointmentReminderSender sender,
        TimeProvider clock,
        ILogger<AppointmentReminderProcessor> logger)
    {
        _db = db;
        _sender = sender;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var due = await _db.AppointmentReminders
            .AsNoTracking()
            .Where(x =>
                x.Status == AppointmentReminderStatus.Pending &&
                x.DueAtUtc <= now)
            .OrderBy(x => x.DueAtUtc)
            .Take(20)
            .Select(x => new
            {
                x.AppointmentId,
                x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimedAt = TrimToMicroseconds(
                _clock.GetUtcNow().UtcDateTime);

            var leaseUntil = claimedAt.AddMinutes(5);

            // Actualización condicional: solamente adquiere
            // un recordatorio que siga pendiente y no haya
            // sido actualizado desde su lectura.
            var claimed = await _db.AppointmentReminders
                .Where(x =>
                    x.AppointmentId == item.AppointmentId &&
                    x.Status == AppointmentReminderStatus.Pending &&
                    x.DueAtUtc <= now &&
                    x.UpdatedAtUtc == item.UpdatedAtUtc)
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(
                            x => x.DueAtUtc,
                            leaseUntil)
                        .SetProperty(
                            x => x.UpdatedAtUtc,
                            claimedAt),
                    cancellationToken);

            if (claimed != 1)
                continue;

            try
            {
                var appointment = await _db.Appointments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.AppointmentId == item.AppointmentId,
                        cancellationToken);

                // Nunca enviamos un recordatorio de una cita
                // que ya fue cancelada o atendida.
                if (appointment is null ||
                    appointment.Status != AppointmentStatus.Pending)
                {
                    await _db.AppointmentReminders
                        .Where(x =>
                            x.AppointmentId == item.AppointmentId &&
                            x.Status == AppointmentReminderStatus.Pending &&
                            x.UpdatedAtUtc == claimedAt &&
                            x.DueAtUtc == leaseUntil)
                        .ExecuteUpdateAsync(
                            update => update.SetProperty(
                                x => x.Status,
                                AppointmentReminderStatus.Cancelled),
                            cancellationToken);

                    continue;
                }

                // Segunda comprobación para evitar usar
                // un recordatorio ya reprogramado.
                var stillPending = await _db.AppointmentReminders
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.AppointmentId == item.AppointmentId &&
                             x.Status == AppointmentReminderStatus.Pending &&
                             x.UpdatedAtUtc == claimedAt &&
                             x.DueAtUtc == leaseUntil,
                        cancellationToken);

                if (!stillPending)
                    continue;

                await _sender.SendAsync(
                    appointment,
                    cancellationToken);

                var completedAt = TrimToMicroseconds(
                    _clock.GetUtcNow().UtcDateTime);

                var completed = await _db.AppointmentReminders
                    .Where(x =>
                        x.AppointmentId == item.AppointmentId &&
                        x.Status == AppointmentReminderStatus.Pending &&
                        x.UpdatedAtUtc == claimedAt &&
                        x.DueAtUtc == leaseUntil)
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(
                                x => x.Status,
                                AppointmentReminderStatus.Sent)
                            .SetProperty(
                                x => x.SentAtUtc,
                                (DateTime?)completedAt)
                            .SetProperty(
                                x => x.UpdatedAtUtc,
                                completedAt),
                        cancellationToken);

                if (completed == 1)
                    sent++;
            }
            catch (Exception ex)
                when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Reminder delivery failed for appointment {AppointmentId}.",
                    item.AppointmentId);

                var retryAt = TrimToMicroseconds(
                    _clock.GetUtcNow().UtcDateTime.AddMinutes(5));

                // Si la cita fue reprogramada mientras
                // enviábamos, esta actualización no afectará
                // el recordatorio nuevo.
                await _db.AppointmentReminders
                    .Where(x =>
                        x.AppointmentId == item.AppointmentId &&
                        x.Status == AppointmentReminderStatus.Pending &&
                        x.UpdatedAtUtc == claimedAt &&
                        x.DueAtUtc == leaseUntil)
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(
                                x => x.DueAtUtc,
                                retryAt)
                            .SetProperty(
                                x => x.UpdatedAtUtc,
                                retryAt),
                        cancellationToken);
            }
        }

        return sent;
    }

    private static DateTime TrimToMicroseconds(DateTime value)
    {
        return value.AddTicks(-(value.Ticks % 10));
    }
}
