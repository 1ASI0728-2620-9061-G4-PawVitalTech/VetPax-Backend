using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

public sealed class AppointmentReminderProcessor
{
    private const int MaxAttempts = 5;

    private static readonly TimeSpan PeruOffset =
        TimeSpan.FromHours(-5);

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

        // Recuperar recordatorios que agotaron sus intentos
        // antes de que el worker pudiera finalizarlos.

        await _db.AppointmentReminders
            .Where(x =>
                x.Status == AppointmentReminderStatus.Pending &&
                x.Attempts >= MaxAttempts &&
                (x.LockedUntilUtc == null ||
                 x.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(
                        x => x.Status,
                        AppointmentReminderStatus.Failed)
                    .SetProperty(
                        x => x.LastError,
                        "Maximum delivery attempts reached.")
                    .SetProperty(
                        x => x.NextAttemptAtUtc,
                        (DateTime?)null)
                    .SetProperty(
                        x => x.LeaseToken,
                        (Guid?)null)
                    .SetProperty(
                        x => x.LockedUntilUtc,
                        (DateTime?)null)
                    .SetProperty(
                        x => x.UpdatedAtUtc,
                        now),
                cancellationToken);

        var candidates = await Eligible(now)
            .AsNoTracking()
            .OrderBy(x => x.DueAtUtc)
            .ThenBy(x => x.AppointmentId)
            .Select(x => x.AppointmentId)
            .Take(20)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var appointmentId in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimedAt = _clock.GetUtcNow().UtcDateTime;

            var leaseToken = Guid.NewGuid();

            var leaseUntil = claimedAt.AddMinutes(5);

            // Adquisición atómica del recordatorio.

            var acquired = await Eligible(claimedAt)
                .Where(x =>
                    x.AppointmentId == appointmentId)
                .ExecuteUpdateAsync(
                    update => update
                        .SetProperty(
                            x => x.LeaseToken,
                            (Guid?)leaseToken)
                        .SetProperty(
                            x => x.LockedUntilUtc,
                            (DateTime?)leaseUntil)
                        .SetProperty(
                            x => x.Attempts,
                            x => x.Attempts + 1)
                        .SetProperty(
                            x => x.UpdatedAtUtc,
                            claimedAt),
                    cancellationToken);

            if (acquired != 1)
                continue;

            try
            {
                var reminder = await _db.AppointmentReminders
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.AppointmentId == appointmentId &&
                            x.LeaseToken == leaseToken,
                        cancellationToken);

                if (reminder is null)
                    continue;

                var appointment = await _db.Appointments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.AppointmentId == appointmentId,
                        cancellationToken);

                // Cancelar avisos de citas que dejaron
                // de estar pendientes.

                if (appointment is null ||
                    appointment.Status != AppointmentStatus.Pending)
                {
                    await CompleteAsync(
                        appointmentId,
                        leaseToken,
                        AppointmentReminderStatus.Cancelled,
                        null,
                        "Appointment is no longer pending.",
                        cancellationToken);

                    continue;
                }

                // Convertir el horario de Perú a UTC.

                var localStart = appointment.AppointmentDate
                    .ToDateTime(appointment.StartTime);

                var startsAtUtc = new DateTimeOffset(
                    localStart,
                    PeruOffset).UtcDateTime;

                // No enviar recordatorios de citas
                // cuya hora de inicio ya pasó.

                if (startsAtUtc <=
                    _clock.GetUtcNow().UtcDateTime)
                {
                    await CompleteAsync(
                        appointmentId,
                        leaseToken,
                        AppointmentReminderStatus.Cancelled,
                        null,
                        "Appointment reminder expired.",
                        cancellationToken);

                    continue;
                }

                // Confirmar que el recordatorio no fue
                // cancelado o reprogramado.

                var stillEligible = await (
                    from r in _db.AppointmentReminders
                    join a in _db.Appointments
                        on r.AppointmentId equals a.AppointmentId
                    where
                        r.AppointmentId == appointmentId &&
                        r.LeaseToken == leaseToken &&
                        r.Status == AppointmentReminderStatus.Pending &&
                        a.Status == AppointmentStatus.Pending &&
                        a.Version == appointment.Version
                    select r.AppointmentId
                ).AnyAsync(cancellationToken);

                if (!stillEligible)
                    continue;

                if (startsAtUtc <=
                    _clock.GetUtcNow().UtcDateTime)
                {
                    await CompleteAsync(
                        appointmentId,
                        leaseToken,
                        AppointmentReminderStatus.Cancelled,
                        null,
                        "Appointment reminder expired.",
                        cancellationToken);

                    continue;
                }

                // Limitar la duración del envío externo.

                using var timeout =
                    CancellationTokenSource
                        .CreateLinkedTokenSource(
                            cancellationToken);

                timeout.CancelAfter(
                    TimeSpan.FromMinutes(2));

                await _sender.SendAsync(
                    appointment,
                    timeout.Token);

                var completed = await CompleteAsync(
                    appointmentId,
                    leaseToken,
                    AppointmentReminderStatus.Sent,
                    null,
                    null,
                    cancellationToken);

                if (completed == 1)
                    sent++;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Si el servidor se detiene, la reserva
                // podrá recuperarse posteriormente.

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Appointment reminder {AppointmentId} " +
                    "delivery failed. Error={ErrorType}",
                    appointmentId,
                    ex.GetType().Name);

                var reminder = await _db.AppointmentReminders
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.AppointmentId == appointmentId &&
                            x.LeaseToken == leaseToken,
                        cancellationToken);

                if (reminder is null)
                    continue;

                var exhausted =
                    reminder.Attempts >= MaxAttempts;

                var nextStatus = exhausted
                    ? AppointmentReminderStatus.Failed
                    : AppointmentReminderStatus.Pending;

                DateTime? retryAt = exhausted
                    ? null
                    : _clock.GetUtcNow().UtcDateTime
                        .AddMinutes(
                            Math.Pow(2, reminder.Attempts));

                await CompleteAsync(
                    appointmentId,
                    leaseToken,
                    nextStatus,
                    retryAt,
                    "FCM delivery unavailable.",
                    cancellationToken);
            }
        }

        return sent;
    }

    private IQueryable<AppointmentReminderRow> Eligible(
        DateTime now)
    {
        return _db.AppointmentReminders
            .Where(x =>
                x.Status == AppointmentReminderStatus.Pending &&
                x.Attempts < MaxAttempts &&
                x.DueAtUtc <= now &&
                (x.NextAttemptAtUtc == null ||
                 x.NextAttemptAtUtc <= now) &&
                (x.LockedUntilUtc == null ||
                 x.LockedUntilUtc <= now));
    }

    private async Task<int> CompleteAsync(
        Guid appointmentId,
        Guid leaseToken,
        AppointmentReminderStatus status,
        DateTime? nextAttemptAt,
        string? error,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        DateTime? sentAt =
            status == AppointmentReminderStatus.Sent
                ? now
                : null;

        return await _db.AppointmentReminders
            .Where(x =>
                x.AppointmentId == appointmentId &&
                x.LeaseToken == leaseToken &&
                x.Status == AppointmentReminderStatus.Pending)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(
                        x => x.Status,
                        status)
                    .SetProperty(
                        x => x.NextAttemptAtUtc,
                        nextAttemptAt)
                    .SetProperty(
                        x => x.LastError,
                        error)
                    .SetProperty(
                        x => x.SentAtUtc,
                        sentAt)
                    .SetProperty(
                        x => x.LeaseToken,
                        (Guid?)null)
                    .SetProperty(
                        x => x.LockedUntilUtc,
                        (DateTime?)null)
                    .SetProperty(
                        x => x.UpdatedAtUtc,
                        now),
                cancellationToken);
    }
}