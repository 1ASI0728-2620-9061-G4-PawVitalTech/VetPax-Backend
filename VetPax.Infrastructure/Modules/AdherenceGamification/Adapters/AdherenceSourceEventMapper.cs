
using VetPax.Application.Modules.AdherenceGamification.Commands;
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Events;
using VetPax.Domain.Modules.MedicationTreatment.Events;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;

public static class AdherenceSourceEventMapper
{
    public static IngestAdherenceEvidenceCommand FromAppointment(
        AppointmentAttendedEvent source,
        Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ownerId == Guid.Empty ||
            source.AppointmentId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid appointment and owner are required.");
        }

        return new IngestAdherenceEvidenceCommand(
            // AppointmentAttendedEvent has no EventId.
            // AppointmentId is stable across outbox retries.
            EventId: source.AppointmentId.Value,
            OwnerId: ownerId,
            Kind: AdherenceEvidenceKind.VeterinaryAppointmentAttended,
            SourceId: source.AppointmentId.Value,
            OccurredAtUtc: AsUtc(source.OccurredAt));
    }

    public static IngestAdherenceEvidenceCommand FromMedication(
        MedicationDoseAdministeredEvent source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.EventId == Guid.Empty ||
            source.OwnerId == Guid.Empty ||
            source.AdministrationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invalid medication administration event.");
        }

        return new IngestAdherenceEvidenceCommand(
            EventId: source.EventId,
            OwnerId: source.OwnerId,
            Kind: AdherenceEvidenceKind.MedicationDoseAdministered,
            SourceId: source.AdministrationId,
            OccurredAtUtc: AsUtc(source.OccurredAtUtc));
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            // Events restored from MySQL may have an
            // unspecified Kind. Their stored time is UTC.
            _ => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc)
        };
    }
}
