
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Events;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;
using Xunit;

namespace VetPax.Tests.Infrastructure.AdherenceGamification;

public sealed class AdherenceSourceEventMapperTests
{
    private static readonly DateTime Now =
        new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AppointmentEvent_MapsOwnerAndSourceCorrectly()
    {
        var appointmentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var source = CreateAppointmentEvent(appointmentId);

        var result =
            AdherenceSourceEventMapper.FromAppointment(
                source,
                ownerId);

        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(appointmentId, result.EventId);
        Assert.Equal(appointmentId, result.SourceId);

        Assert.Equal(
            AdherenceEvidenceKind.VeterinaryAppointmentAttended,
            result.Kind);

        Assert.Equal(Now, result.OccurredAtUtc);
    }

    [Fact]
    public void AppointmentRetry_UsesSameEventId()
    {
        var appointmentId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var source = CreateAppointmentEvent(appointmentId);

        var first =
            AdherenceSourceEventMapper.FromAppointment(
                source,
                ownerId);

        var retry =
            AdherenceSourceEventMapper.FromAppointment(
                source,
                ownerId);

        Assert.Equal(first.EventId, retry.EventId);
        Assert.Equal(first.SourceId, retry.SourceId);
    }

    [Fact]
    public void MedicationEvent_UsesAdministrationId()
    {
        var eventId = Guid.NewGuid();
        var administrationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var source = new MedicationDoseAdministeredEvent(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            administrationId,
            Guid.NewGuid(),
            ownerId,
            Now,
            Now,
            Now);

        var result =
            AdherenceSourceEventMapper.FromMedication(source);

        Assert.Equal(eventId, result.EventId);
        Assert.Equal(administrationId, result.SourceId);
        Assert.Equal(ownerId, result.OwnerId);

        Assert.Equal(
            AdherenceEvidenceKind.MedicationDoseAdministered,
            result.Kind);
    }

    [Fact]
    public void AppointmentWithoutOwner_IsRejected()
    {
        var source = CreateAppointmentEvent(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            AdherenceSourceEventMapper.FromAppointment(
                source,
                Guid.Empty));
    }

    [Fact]
    public void RestoredUtcTimestamp_IsNormalized()
    {
        var source = new MedicationDoseAdministeredEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now,
            Now,
            DateTime.SpecifyKind(
                Now,
                DateTimeKind.Unspecified));

        var result =
            AdherenceSourceEventMapper.FromMedication(source);

        Assert.Equal(DateTimeKind.Utc,
            result.OccurredAtUtc.Kind);

        Assert.Equal(Now, result.OccurredAtUtc);
    }

    private static AppointmentAttendedEvent CreateAppointmentEvent(
        Guid appointmentId)
    {
        return new AppointmentAttendedEvent(
            new AppointmentId(appointmentId),
            new AppointmentPetId(Guid.NewGuid()),
            new AppointmentClinicId(Guid.NewGuid()),
            new AppointmentVeterinarianId(Guid.NewGuid()),
            Guid.NewGuid(),
            Now);
    }
}
