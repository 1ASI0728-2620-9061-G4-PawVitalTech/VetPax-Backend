using VetPax.Application.Modules.AdherenceGamification.Commands;
using VetPax.Application.Modules.AdherenceGamification.Handlers;
using VetPax.Domain.Modules.AdherenceGamification.Enums;

namespace VetPax.Application.Modules.AdherenceGamification.Services;

public sealed class AdherenceEvidenceConsumer
{
    private readonly IngestAdherenceEvidenceHandler _handler;

    public AdherenceEvidenceConsumer(
        IngestAdherenceEvidenceHandler handler)
    {
        _handler = handler;
    }

    public Task<bool> OnAppointmentAttendedAsync(
        Guid eventId,
        Guid ownerId,
        Guid appointmentId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        return _handler.HandleAsync(
            new IngestAdherenceEvidenceCommand(
                eventId,
                ownerId,
                AdherenceEvidenceKind.VeterinaryAppointmentAttended,
                appointmentId,
                occurredAtUtc),
            cancellationToken);
    }

    public Task<bool> OnDoseAdministeredAsync(
        Guid eventId,
        Guid ownerId,
        Guid administrationId,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        return _handler.HandleAsync(
            new IngestAdherenceEvidenceCommand(
                eventId,
                ownerId,
                AdherenceEvidenceKind.MedicationDoseAdministered,
                administrationId,
                occurredAtUtc),
            cancellationToken);
    }
}