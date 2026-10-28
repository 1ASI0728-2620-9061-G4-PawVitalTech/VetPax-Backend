
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AdherenceGamification.Handlers;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Events;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Messaging;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;

public sealed class AdherenceDomainEventPublisher
    : IDomainEventPublisher
{
    private readonly DevelopmentDomainEventPublisher _development;
    private readonly IngestAdherenceEvidenceHandler _ingest;
    private readonly VetPaxDbContext _db;
    private readonly ILogger<AdherenceDomainEventPublisher> _logger;

    public AdherenceDomainEventPublisher(
        DevelopmentDomainEventPublisher development,
        IngestAdherenceEvidenceHandler ingest,
        VetPaxDbContext db,
        ILogger<AdherenceDomainEventPublisher> logger)
    {
        _development = development;
        _ingest = ingest;
        _db = db;
        _logger = logger;
    }

    public async Task PublishAsync(
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Preserve existing development logging.
        await _development.PublishAsync(
            events,
            cancellationToken);

        foreach (var domainEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (domainEvent)
            {
                case AppointmentAttendedEvent appointment:
                    await HandleAppointmentAsync(
                        appointment,
                        cancellationToken);
                    break;

                case MedicationDoseAdministeredEvent medication:
                    await HandleMedicationAsync(
                        medication,
                        cancellationToken);
                    break;

                // Other BC events keep their existing
                // development-publisher behavior.
                default:
                    break;
            }
        }
    }

    private async Task HandleAppointmentAsync(
        AppointmentAttendedEvent appointment,
        CancellationToken cancellationToken)
    {
        // AppointmentAttendedEvent doesn't contain OwnerId.
        // Get it from the persisted appointment.
        var ownerId = await _db.Appointments
            .AsNoTracking()
            .Where(x =>
                x.AppointmentId == appointment.AppointmentId.Value &&
                x.PetId == appointment.PetId.Value &&
                x.Status == AppointmentStatus.Attended &&
                x.ClinicalAttentionId == appointment.ClinicalAttentionId)
            .Select(x => (Guid?)x.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!ownerId.HasValue ||
            ownerId.Value == Guid.Empty)
        {
            // Do not acknowledge an event we could not
            // correctly attribute. The source Outbox
            // must be allowed to retry.
            throw new InvalidOperationException(
                "The attended appointment has no valid owner.");
        }

        var command =
            AdherenceSourceEventMapper.FromAppointment(
                appointment,
                ownerId.Value);

        var processed = await _ingest.HandleAsync(
            command,
            cancellationToken);

        _logger.LogInformation(
            "Appointment adherence evidence {AppointmentId}: {Result}",
            appointment.AppointmentId.Value,
            processed ? "Processed" : "AlreadyProcessed");
    }

    private async Task HandleMedicationAsync(
        MedicationDoseAdministeredEvent medication,
        CancellationToken cancellationToken)
    {
        var command =
            AdherenceSourceEventMapper.FromMedication(
                medication);

        var processed = await _ingest.HandleAsync(
            command,
            cancellationToken);

        _logger.LogInformation(
            "Medication adherence evidence {AdministrationId}: {Result}",
            medication.AdministrationId,
            processed ? "Processed" : "AlreadyProcessed");
    }
}
