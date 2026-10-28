using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

public sealed class MedicationFirebaseNotificationSender
    : IMedicationNotificationSender
{
    private readonly IMedicationPushGateway _gateway;

    public MedicationFirebaseNotificationSender(
        IMedicationPushGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task SendAsync(
        MedicationNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await _gateway.SendDoseReminderAsync(
            message.OwnerId,
            message.DoseId,
            cancellationToken);
    }
}