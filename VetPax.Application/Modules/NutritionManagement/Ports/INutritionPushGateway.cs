namespace VetPax.Application.Modules.NutritionManagement.Ports;

public interface INutritionPushGateway
{
    Task SendAsync(
        Guid ownerId,
        Guid planId,
        Guid reminderId,
        CancellationToken cancellationToken = default);
}