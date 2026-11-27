
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;

public sealed class ClinicalAppointmentLinkAdapter
    : IClinicalAppointmentLinkPort
{
    private readonly VetPaxDbContext _db;

    public ClinicalAppointmentLinkAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task StageVerifiedLinkAsync(
        Guid appointmentId,
        Guid clinicalAttentionId,
        PetId petId,
        ClinicId clinicId,
        VeterinarianId veterinarianId,
        CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty ||
            clinicalAttentionId == Guid.Empty ||
            petId.Value == Guid.Empty ||
            clinicId.Value == Guid.Empty ||
            veterinarianId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid appointment and clinical identifiers are required.");
        }

        var appointment = await _db.Appointments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.AppointmentId == appointmentId,
                cancellationToken);

        if (appointment is null)
        {
            throw new KeyNotFoundException(
                "Appointment not found.");
        }

        if (appointment.PetId != petId.Value ||
            appointment.ClinicId != clinicId.Value ||
            appointment.VeterinarianId != veterinarianId.Value)
        {
            throw new UnauthorizedAccessException(
                "Appointment does not match the clinical context.");
        }

        if (appointment.Status != AppointmentStatus.Pending)
        {
            throw new ClinicalAppointmentLinkConflictException(
                "Only pending appointments can receive a clinical attention link.");
        }

        var alreadyLinked = await _db.AppointmentAttentionLinks
            .AsNoTracking()
            .AnyAsync(
                x => x.AppointmentId == appointmentId ||
                     x.ClinicalAttentionId == clinicalAttentionId,
                cancellationToken);

        if (alreadyLinked)
        {
            throw new ClinicalAppointmentLinkConflictException(
                "Appointment or clinical attention is already linked.");
        }

        // Stage the appointment status change in the same unit of work as the attention.
        // A successful clinical-attention registration must not leave the appointment Pending.
        var trackedAppointment = await _db.Appointments.SingleAsync(
            x => x.AppointmentId == appointmentId, cancellationToken);
        if (trackedAppointment.Status != AppointmentStatus.Pending)
            throw new ClinicalAppointmentLinkConflictException(
                "Appointment is no longer pending.");

        trackedAppointment.Status = AppointmentStatus.Attended;
        trackedAppointment.ClinicalAttentionId = clinicalAttentionId;
        trackedAppointment.UpdatedAt = DateTime.UtcNow;
        trackedAppointment.Version++;

        var reminder = await _db.AppointmentReminders.SingleOrDefaultAsync(
            x => x.AppointmentId == appointmentId, cancellationToken);
        if (reminder is not null && reminder.Status == AppointmentReminderStatus.Pending)
        {
            reminder.Status = AppointmentReminderStatus.Cancelled;
            reminder.UpdatedAtUtc = DateTime.UtcNow;
        }

        _db.AppointmentAttentionLinks.Add(
            new AppointmentAttentionLinkRow
            {
                AppointmentId = appointmentId,
                ClinicalAttentionId = clinicalAttentionId,
                LinkedAtUtc = DateTime.UtcNow
            });

        // No guardar aquí: el handler de US03 confirma
        // atención + vínculo mediante el mismo UnitOfWork.
    }
}
