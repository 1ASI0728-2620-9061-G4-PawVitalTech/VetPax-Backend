
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Events;
using VetPax.Domain.Modules.AppointmentManagement.Exceptions;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Domain.AppointmentManagement;

public sealed class AppointmentDomainTests
{
    private static Appointment CreateAppointment()
    {
        return Appointment.Schedule(
            new AppointmentOwnerId(Guid.NewGuid()),
            new AppointmentPetId(Guid.NewGuid()),
            new AppointmentClinicId(Guid.NewGuid()),
            new AppointmentVeterinarianId(Guid.NewGuid()),
            new AppointmentSlot(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                new TimeOnly(9, 0)),
            "Control veterinario",
            DateTime.UtcNow);
    }

    [Fact]
    public void Schedule_CreatesPendingAppointment()
    {
        var appointment = CreateAppointment();

        Assert.NotEqual(
            Guid.Empty,
            appointment.AppointmentId.Value);

        Assert.Equal(
            AppointmentStatus.Pending,
            appointment.Status);

        Assert.Null(appointment.ClinicalAttentionId);

        Assert.Contains(
            appointment.PullDomainEvents(),
            e => e is AppointmentScheduledEvent);
    }

    [Fact]
    public void Reschedule_UpdatesSlotWithoutChangingId()
    {
        var appointment = CreateAppointment();
        appointment.PullDomainEvents();

        var originalId = appointment.AppointmentId;

        var newSlot = new AppointmentSlot(
            appointment.Slot.Date,
            new TimeOnly(10, 0));

        appointment.Reschedule(
            newSlot,
            DateTime.UtcNow);

        Assert.Equal(originalId, appointment.AppointmentId);
        Assert.Equal(newSlot, appointment.Slot);

        Assert.Contains(
            appointment.PullDomainEvents(),
            e => e is AppointmentRescheduledEvent);
    }

    [Fact]
    public void Cancel_ChangesStatusAndReleasesSlot()
    {
        var appointment = CreateAppointment();
        appointment.PullDomainEvents();

        appointment.Cancel(DateTime.UtcNow);

        Assert.Equal(
            AppointmentStatus.Cancelled,
            appointment.Status);

        Assert.Contains(
            appointment.PullDomainEvents(),
            e => e is AppointmentCancelledEvent);
    }

    [Fact]
    public void CancelledAppointment_CannotBeRescheduled()
    {
        var appointment = CreateAppointment();

        appointment.Cancel(DateTime.UtcNow);

        var newSlot = new AppointmentSlot(
            appointment.Slot.Date,
            new TimeOnly(11, 0));

        Assert.Throws<AppointmentStateException>(
            () => appointment.Reschedule(
                newSlot,
                DateTime.UtcNow));
    }

    [Fact]
    public void MarkAttended_RequiresAssignedVeterinarian()
    {
        var appointment = CreateAppointment();

        Assert.Throws<UnauthorizedAccessException>(
            () => appointment.MarkAttended(
                Guid.NewGuid(),
                new AppointmentVeterinarianId(Guid.NewGuid()),
                DateTime.UtcNow));

        Assert.Equal(
            AppointmentStatus.Pending,
            appointment.Status);
    }

    [Fact]
    public void MarkAttended_ReferencesClinicalAttention()
    {
        var appointment = CreateAppointment();
        appointment.PullDomainEvents();

        var attentionId = Guid.NewGuid();

        appointment.MarkAttended(
            attentionId,
            appointment.VeterinarianId,
            DateTime.UtcNow);

        Assert.Equal(
            AppointmentStatus.Attended,
            appointment.Status);

        Assert.Equal(
            attentionId,
            appointment.ClinicalAttentionId);

        Assert.Contains(
            appointment.PullDomainEvents(),
            e => e is AppointmentAttendedEvent attended &&
                 attended.ClinicalAttentionId == attentionId);
    }

    [Fact]
    public void Rehydrate_DoesNotProduceNewEvents()
    {
        var original = CreateAppointment();

        var restored = Appointment.Rehydrate(
            original.AppointmentId,
            original.OwnerId,
            original.PetId,
            original.ClinicId,
            original.VeterinarianId,
            original.Slot,
            original.Reason,
            original.Status,
            original.ClinicalAttentionId,
            original.CreatedAt,
            original.UpdatedAt);

        Assert.Equal(
            original.AppointmentId,
            restored.AppointmentId);

        Assert.Empty(restored.PullDomainEvents());
    }

    [Fact]
    public void Slots_DetectOverlappingTimes()
    {
        var date = DateOnly.FromDateTime(
            DateTime.UtcNow.AddDays(1));

        var first = new AppointmentSlot(
            date,
            new TimeOnly(9, 0));

        var same = new AppointmentSlot(
            date,
            new TimeOnly(9, 0));

        var next = new AppointmentSlot(
            date,
            new TimeOnly(9, 30));

        Assert.True(first.OverlapsWith(same));
        Assert.False(first.OverlapsWith(next));
    }
}
