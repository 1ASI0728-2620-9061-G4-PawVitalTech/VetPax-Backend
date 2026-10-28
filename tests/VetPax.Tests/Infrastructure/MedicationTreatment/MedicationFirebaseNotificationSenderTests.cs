
using System.Text.Json;
using NSubstitute;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;
using Xunit;

namespace VetPax.Tests.Infrastructure.MedicationTreatment;

public sealed class MedicationFirebaseNotificationSenderTests
{
    [Fact]
    public async Task SendAsync_DelegatesToFirebaseGateway()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var doseId = Guid.NewGuid();

        var message = CreateMessage(ownerId, doseId);

        var gateway = Substitute.For<IMedicationPushGateway>();

        gateway.SendDoseReminderAsync(
                ownerId,
                doseId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                new MedicationPushResult(1, 1, 0)));

        var sender =
            new MedicationFirebaseNotificationSender(gateway);

        // Act
        await sender.SendAsync(message);

        // Assert
        await gateway.Received(1)
            .SendDoseReminderAsync(
                ownerId,
                doseId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_PropagatesFirebaseFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var doseId = Guid.NewGuid();

        var message = CreateMessage(ownerId, doseId);

        var gateway = Substitute.For<IMedicationPushGateway>();

        gateway.SendDoseReminderAsync(
                ownerId,
                doseId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MedicationPushResult>(
                new InvalidOperationException(
                    "Firebase delivery failed.")));

        var sender =
            new MedicationFirebaseNotificationSender(gateway);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(message));
    }

    [Fact]
    public async Task SendAsync_RejectsNullMessage()
    {
        // Arrange
        var gateway = Substitute.For<IMedicationPushGateway>();

        var sender =
            new MedicationFirebaseNotificationSender(gateway);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.SendAsync(null!));

        await gateway.DidNotReceive()
            .SendDoseReminderAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ForwardsCancellationToken()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var doseId = Guid.NewGuid();

        var message = CreateMessage(ownerId, doseId);

        var gateway = Substitute.For<IMedicationPushGateway>();

        gateway.SendDoseReminderAsync(
                ownerId,
                doseId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                new MedicationPushResult(1, 1, 0)));

        var sender =
            new MedicationFirebaseNotificationSender(gateway);

        using var cancellation =
            new CancellationTokenSource();

        // Act
        await sender.SendAsync(
            message,
            cancellation.Token);

        // Assert
        await gateway.Received(1)
            .SendDoseReminderAsync(
                ownerId,
                doseId,
                cancellation.Token);
    }

    private static MedicationNotificationMessage CreateMessage(
        Guid ownerId,
        Guid doseId)
    {
        // Construye el mensaje sin depender de la cantidad
        // de parametros de su constructor posicional.
        // Las propiedades adicionales usan sus valores
        // predeterminados durante la deserializacion.
        var json = JsonSerializer.Serialize(new
        {
            OwnerId = ownerId,
            DoseId = doseId
        });

        return JsonSerializer.Deserialize<MedicationNotificationMessage>(
                   json)
               ?? throw new InvalidOperationException(
                   "Could not create medication notification message.");
    }
}
