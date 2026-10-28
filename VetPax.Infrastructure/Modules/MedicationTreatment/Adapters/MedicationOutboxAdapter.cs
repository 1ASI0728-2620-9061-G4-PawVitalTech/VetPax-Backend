
using System.Text.Json;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Adapters;

public sealed class MedicationOutboxAdapter
    : IMedicationOutboxPort
{
    private readonly VetPaxDbContext _db;

    public MedicationOutboxAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public Task StageAsync(
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var domainEvent in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var eventId = domainEvent switch
            {
                MedicationDoseAdministeredEvent administered =>
                    administered.EventId,

                _ => Guid.NewGuid()
            };

            var planId = domainEvent switch
            {
                MedicationPlanPrescribedEvent x => x.PlanId,
                MedicationScheduleChangedEvent x => x.PlanId,
                MedicationRemindersConfiguredEvent x => x.PlanId,
                MedicationDoseScheduledEvent x => x.PlanId,
                MedicationReminderGeneratedEvent x => x.PlanId,
                MedicationDoseAdministeredEvent x => x.PlanId,
                MedicationPlanClosedEvent x => x.PlanId,
                _ => throw new InvalidOperationException(
                    "Unsupported medication domain event.")
            };

            var payload = JsonSerializer.Serialize(
                domainEvent,
                domainEvent.GetType(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            _db.Set<MedicationOutboxRow>().Add(
                new MedicationOutboxRow
                {
                    EventId = eventId,
                    PlanId = planId,
                    EventType =
                        domainEvent.GetType().FullName!,
                    Payload = payload,
                    OccurredAtUtc = domainEvent.OccurredAt,
                    NextAttemptAtUtc = DateTime.UtcNow,
                    PublishedAtUtc = null,
                    Attempts = 0,
                    LastError = null
                });
        }

        // No publicar ni confirmar aquí.
        return Task.CompletedTask;
    }
}
