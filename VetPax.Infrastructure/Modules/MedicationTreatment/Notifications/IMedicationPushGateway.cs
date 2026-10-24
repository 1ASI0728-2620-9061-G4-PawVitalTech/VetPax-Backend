namespace VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

public interface IMedicationPushGateway
{
    Task<MedicationPushResult> SendDoseReminderAsync(
        Guid ownerId,
        Guid doseId,
        CancellationToken cancellationToken = default);
}