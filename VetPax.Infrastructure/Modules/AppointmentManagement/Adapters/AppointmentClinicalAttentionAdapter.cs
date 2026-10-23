
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;

public sealed class AppointmentClinicalAttentionAdapter
    : IAppointmentClinicalAttentionPort
{
    private readonly VetPaxDbContext _db;

    public AppointmentClinicalAttentionAdapter(
        VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsLinkedToAppointmentAsync(
        Guid attentionId,
        Guid appointmentId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default)
    {
        if (attentionId == Guid.Empty ||
            appointmentId == Guid.Empty ||
            petId == Guid.Empty ||
            clinicId == Guid.Empty ||
            veterinarianId == Guid.Empty)
        {
            return false;
        }

        var appointmentMatches = await _db.Appointments
            .AsNoTracking()
            .AnyAsync(
                x => x.AppointmentId == appointmentId &&
                     x.PetId == petId &&
                     x.ClinicId == clinicId &&
                     x.VeterinarianId == veterinarianId,
                cancellationToken);

        if (!appointmentMatches)
            return false;

        var linked = await _db.AppointmentAttentionLinks
            .AsNoTracking()
            .AnyAsync(
                x => x.AppointmentId == appointmentId &&
                     x.ClinicalAttentionId == attentionId,
                cancellationToken);

        if (!linked)
            return false;

        var record = await _db.ClinicalRecords
            .AsNoTracking()
            .Include(x => x.Attentions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                x => x.PetId == new PetId(petId),
                cancellationToken);

        if (record is null)
            return false;

        return record.Attentions.Any(
            a => a.AttentionId.Value == attentionId &&
                 a.ClinicId.Value == clinicId &&
                 a.VeterinarianId.Value == veterinarianId);
    }
}
