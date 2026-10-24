
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.Events;
using VetPax.Domain.Modules.AppointmentManagement.Exceptions;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.AppointmentManagement.Aggregates;

public sealed class Appointment
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public AppointmentId AppointmentId { get; private set; }
    public AppointmentOwnerId OwnerId { get; private set; }
    public AppointmentPetId PetId { get; private set; }
    public AppointmentClinicId ClinicId { get; private set; }
    public AppointmentVeterinarianId VeterinarianId { get; private set; }

    public AppointmentSlot Slot { get; private set; } = null!;
    public string Reason { get; private set; } = "";
    public AppointmentStatus Status { get; private set; }

    public Guid? ClinicalAttentionId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Appointment(
        AppointmentId appointmentId,
        AppointmentOwnerId ownerId,
        AppointmentPetId petId,
        AppointmentClinicId clinicId,
        AppointmentVeterinarianId veterinarianId,
        AppointmentSlot slot,
        string reason,
        DateTime createdAt)
    {
        if (appointmentId.Value == Guid.Empty ||
            ownerId.Value == Guid.Empty ||
            petId.Value == Guid.Empty ||
            clinicId.Value == Guid.Empty ||
            veterinarianId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "All appointment identifiers are required.");
        }

        ArgumentNullException.ThrowIfNull(slot);

        AppointmentId = appointmentId;
        OwnerId = ownerId;
        PetId = petId;
        ClinicId = clinicId;
        VeterinarianId = veterinarianId;
        Slot = slot;
        Reason = ValidateReason(reason);
        Status = AppointmentStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static Appointment Schedule(
        AppointmentOwnerId ownerId,
        AppointmentPetId petId,
        AppointmentClinicId clinicId,
        AppointmentVeterinarianId veterinarianId,
        AppointmentSlot slot,
        string reason,
        DateTime occurredAt)
    {
        var appointment = new Appointment(
            AppointmentId.New(),
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            slot,
            reason,
            occurredAt);

        appointment._domainEvents.Add(
            new AppointmentScheduledEvent(
                appointment.AppointmentId,
                appointment.PetId,
                appointment.ClinicId,
                appointment.VeterinarianId,
                appointment.Slot,
                occurredAt));

        return appointment;
    }

    public bool IsOwnedBy(AppointmentOwnerId ownerId)
    {
        return ownerId.Value != Guid.Empty &&
               OwnerId == ownerId;
    }

    public bool IsAssignedTo(
        AppointmentVeterinarianId veterinarianId)
    {
        return veterinarianId.Value != Guid.Empty &&
               VeterinarianId == veterinarianId;
    }

    public bool IsPending()
    {
        return Status == AppointmentStatus.Pending;
    }

    public void Reschedule(
        AppointmentSlot newSlot,
        DateTime occurredAt)
    {
        EnsurePending();

        ArgumentNullException.ThrowIfNull(newSlot);

        if (Slot == newSlot)
        {
            throw new AppointmentStateException(
                "The new schedule must be different.");
        }

        var previousSlot = Slot;

        Slot = newSlot;
        UpdatedAt = occurredAt;

        _domainEvents.Add(
            new AppointmentRescheduledEvent(
                AppointmentId,
                previousSlot,
                newSlot,
                occurredAt));
    }

    public void Cancel(DateTime occurredAt)
    {
        EnsurePending();

        var releasedSlot = Slot;

        Status = AppointmentStatus.Cancelled;
        UpdatedAt = occurredAt;

        _domainEvents.Add(
            new AppointmentCancelledEvent(
                AppointmentId,
                releasedSlot,
                occurredAt));
    }

    public void MarkAttended(
        Guid clinicalAttentionId,
        AppointmentVeterinarianId actingVeterinarianId,
        DateTime occurredAt)
    {
        EnsurePending();

        if (clinicalAttentionId == Guid.Empty)
        {
            throw new ArgumentException(
                "A clinical attention ID is required.");
        }

        if (!IsAssignedTo(actingVeterinarianId))
        {
            throw new UnauthorizedAccessException(
                "Only the assigned veterinarian can attend.");
        }

        ClinicalAttentionId = clinicalAttentionId;
        Status = AppointmentStatus.Attended;
        UpdatedAt = occurredAt;

        _domainEvents.Add(
            new AppointmentAttendedEvent(
                AppointmentId,
                PetId,
                ClinicId,
                VeterinarianId,
                clinicalAttentionId,
                occurredAt));
    }

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }

    public static Appointment Rehydrate(
        AppointmentId appointmentId,
        AppointmentOwnerId ownerId,
        AppointmentPetId petId,
        AppointmentClinicId clinicId,
        AppointmentVeterinarianId veterinarianId,
        AppointmentSlot slot,
        string reason,
        AppointmentStatus status,
        Guid? clinicalAttentionId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException(
                "Invalid appointment status.");
        }

        if (status == AppointmentStatus.Attended &&
            (!clinicalAttentionId.HasValue ||
             clinicalAttentionId.Value == Guid.Empty))
        {
            throw new ArgumentException(
                "Attended appointment requires a clinical attention.");
        }

        if (status != AppointmentStatus.Attended &&
            clinicalAttentionId.HasValue)
        {
            throw new ArgumentException(
                "Only attended appointments can reference an attention.");
        }

        var appointment = new Appointment(
            appointmentId,
            ownerId,
            petId,
            clinicId,
            veterinarianId,
            slot,
            reason,
            createdAt);

        appointment.Status = status;
        appointment.ClinicalAttentionId = clinicalAttentionId;
        appointment.UpdatedAt = updatedAt;

        return appointment;
    }

    private void EnsurePending()
    {
        if (!IsPending())
        {
            throw new AppointmentStateException(
                "Only pending appointments can be modified.");
        }
    }

    private static string ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Appointment reason is required.");
        }

        var normalized = reason.Trim();

        if (normalized.Length > 500)
        {
            throw new ArgumentException(
                "Appointment reason cannot exceed 500 characters.");
        }

        return normalized;
    }
}
