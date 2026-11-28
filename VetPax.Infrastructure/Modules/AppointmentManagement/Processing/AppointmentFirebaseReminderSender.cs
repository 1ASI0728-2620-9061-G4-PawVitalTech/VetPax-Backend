using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Processing;

public sealed class AppointmentFirebaseReminderSender
    : IAppointmentReminderSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<AppointmentFirebaseReminderSender> _logger;

    public AppointmentFirebaseReminderSender(
        IServiceScopeFactory scopeFactory,
        FirebaseMessaging messaging,
        ILogger<AppointmentFirebaseReminderSender> logger)
    {
        _scopeFactory = scopeFactory;
        _messaging = messaging;
        _logger = logger;
    }

    public async Task SendAsync(
        AppointmentRow appointment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        if (appointment.OwnerId == Guid.Empty ||
            appointment.AppointmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid owner and appointment IDs are required.");
        }

        // Scope independiente para gestionar los tokens
        // sin afectar el DbContext del procesador de citas.
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var tokenStore = scope.ServiceProvider
            .GetRequiredService<MedicationDeviceTokenStore>();

        var devices = await tokenStore.GetActiveTokensAsync(
            appointment.OwnerId,
            cancellationToken);

        if (devices.Count == 0)
        {
            throw new InvalidOperationException(
                "No active FCM devices are registered for this owner.");
        }

        var accepted = 0;
        var invalidated = 0;
        var failures = 0;

        var deliveryKey =
            $"{appointment.AppointmentId:N}-{appointment.UpdatedAt.Ticks}";

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = new Message
            {
                Token = device.Token,

                Notification = new Notification
                {
                    Title = "Recordatorio de cita veterinaria",

                    Body = "Tu mascota tiene una próxima cita. " +
                           "Abre VetPax para consultar los detalles."
                },

                Data = new Dictionary<string, string>
                {
                    ["type"] = "appointment_reminder",

                    ["appointmentId"] =
                        appointment.AppointmentId.ToString("D"),

                    ["deliveryKey"] = deliveryKey
                }
            };

            try
            {
                await _messaging.SendAsync(
                    message,
                    cancellationToken);

                accepted++;
            }
            catch (FirebaseMessagingException exception)
                when (exception.MessagingErrorCode ==
                      MessagingErrorCode.Unregistered)
            {
                await tokenStore.DisableAsync(
                    appointment.OwnerId,
                    device.DeviceId,
                    cancellationToken);

                invalidated++;

                _logger.LogWarning(
                    "An unregistered FCM device was disabled.");
            }
            catch (FirebaseMessagingException exception)
            {
                failures++;

                _logger.LogWarning(
                    "Appointment FCM delivery failed. Code={Code}",
                    exception.MessagingErrorCode);
            }
        }

        _logger.LogInformation(
            "Appointment reminder {AppointmentId}: " +
            "Accepted={Accepted}, Invalidated={Invalidated}, " +
            "Failures={Failures}.",
            appointment.AppointmentId,
            accepted,
            invalidated,
            failures);

        if (accepted == 0)
        {
            throw new InvalidOperationException(
                "Firebase did not accept the appointment reminder.");
        }
    }
}