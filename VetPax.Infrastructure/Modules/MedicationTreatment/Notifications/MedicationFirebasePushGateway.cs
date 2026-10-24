
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

public sealed record MedicationPushResult(
    int Attempted,
    int AcceptedByFcm,
    int InvalidatedTokens);

public sealed class MedicationFirebasePushGateway : IMedicationPushGateway
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<MedicationFirebasePushGateway> _logger;

    public MedicationFirebasePushGateway(
        IServiceScopeFactory scopeFactory,
        FirebaseMessaging messaging,
        ILogger<MedicationFirebasePushGateway> logger)
    {
        _scopeFactory = scopeFactory;
        _messaging = messaging;
        _logger = logger;
    }

    public async Task<MedicationPushResult> SendDoseReminderAsync(
        Guid ownerId,
        Guid doseId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty || doseId == Guid.Empty)
            throw new ArgumentException(
                "Valid owner and dose IDs are required.");

        // Un scope independiente evita que la actualización
        // de tokens confirme accidentalmente otros cambios
        // pendientes en el DbContext del worker.
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var tokenStore = scope.ServiceProvider
            .GetRequiredService<MedicationDeviceTokenStore>();

        var devices = await tokenStore.GetActiveTokensAsync(
            ownerId,
            cancellationToken);

        if (devices.Count == 0)
        {
            throw new InvalidOperationException(
                "No active FCM devices are registered for this owner.");
        }

        var accepted = 0;
        var invalidated = 0;
        var failures = 0;

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = new Message
            {
                Token = device.Token,
                Notification = new Notification
                {
                    Title = "VetPax",
                    Body = "Tienes una dosis programada. " +
                           "Abre la aplicación para ver los detalles."
                },
                Data = new Dictionary<string, string>
                {
                    ["type"] = "medication_reminder",
                    ["doseId"] = doseId.ToString("D")
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
                    ownerId,
                    device.DeviceId,
                    cancellationToken);

                invalidated++;
            }
            catch (FirebaseMessagingException exception)
            {
                failures++;

                // No registrar el token en logs.
                _logger.LogWarning(
                    "FCM rejected a medication reminder. Code={Code}",
                    exception.MessagingErrorCode);
            }
        }

        // Nunca reportar éxito cuando Firebase
        // no aceptó ningún mensaje.
        if (accepted == 0 || failures > 0)
        {
            throw new InvalidOperationException(
                "Medication reminder delivery to FCM was incomplete.");
        }

        return new MedicationPushResult(
            devices.Count,
            accepted,
            invalidated);
    }
}
