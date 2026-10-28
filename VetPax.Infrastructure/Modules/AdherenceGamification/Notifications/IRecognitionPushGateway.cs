namespace VetPax.Infrastructure.Modules.AdherenceGamification.Notifications;

public interface IRecognitionPushGateway
{
    Task SendAsync(
        Guid ownerId,
        Guid recognitionId,
        string newLevel,
        CancellationToken cancellationToken = default);
}