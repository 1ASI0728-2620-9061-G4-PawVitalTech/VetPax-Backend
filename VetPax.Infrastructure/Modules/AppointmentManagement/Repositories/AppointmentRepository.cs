
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.ReadModels;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Repositories;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly VetPaxDbContext _db;

    public AppointmentRepository(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<Appointment?> FindByIdAsync(
        AppointmentId appointmentId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Appointments
            .SingleOrDefaultAsync(
                x => x.AppointmentId == appointmentId.Value,
                cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    public async Task SaveAsync(
        Appointment appointment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        var row = await _db.Appointments
            .SingleOrDefaultAsync(
                x => x.AppointmentId ==
                     appointment.AppointmentId.Value,
                cancellationToken);

        if (row is null)
        {
            row = new AppointmentRow
            {
                AppointmentId = appointment.AppointmentId.Value,
                Version = 1
            };

            CopyToRow(appointment, row);

            _db.Appointments.Add(row);
        }
        else
        {
            CopyToRow(appointment, row);

            // EF Core utiliza el valor original como
            // concurrency token durante el UPDATE.
            row.Version++;
        }
    }

    public async Task<bool> HasOverlapAsync(
        AppointmentVeterinarianId veterinarianId,
        AppointmentSlot slot,
        AppointmentId? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var excludedId = excludeAppointmentId?.Value;

        return await _db.Appointments
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.VeterinarianId == veterinarianId.Value &&
                    x.AppointmentDate == slot.Date &&
                    x.Status != AppointmentStatus.Cancelled &&
                    x.StartTime < slot.EndTime &&
                    x.EndTime > slot.StartTime &&
                    (!excludedId.HasValue ||
                     x.AppointmentId != excludedId.Value),
                cancellationToken);
    }

    public async Task<IReadOnlyList<AgendaAppointmentEntry>>
        FindAgendaAsync(
            AppointmentVeterinarianId veterinarianId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ArgumentException(
                "End date cannot precede start date.");
        }

        var rows = await _db.Appointments
            .AsNoTracking()
            .Where(x =>
                x.VeterinarianId == veterinarianId.Value &&
                x.AppointmentDate >= from &&
                x.AppointmentDate <= to)
            .OrderBy(x => x.AppointmentDate)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new AgendaAppointmentEntry(
            new AppointmentId(x.AppointmentId),
            new AppointmentPetId(x.PetId),
            new AppointmentClinicId(x.ClinicId),
            new AppointmentVeterinarianId(x.VeterinarianId),
            new AppointmentSlot(
                x.AppointmentDate,
                x.StartTime),
            x.Reason,
            x.Status,
            x.ClinicalAttentionId
        )).ToArray();
    }

    private static Appointment ToDomain(
        AppointmentRow row)
    {
        return Appointment.Rehydrate(
            new AppointmentId(row.AppointmentId),
            new AppointmentOwnerId(row.OwnerId),
            new AppointmentPetId(row.PetId),
            new AppointmentClinicId(row.ClinicId),
            new AppointmentVeterinarianId(row.VeterinarianId),
            new AppointmentSlot(
                row.AppointmentDate,
                row.StartTime),
            row.Reason,
            row.Status,
            row.ClinicalAttentionId,
            row.CreatedAt,
            row.UpdatedAt
        );
    }

    private static void CopyToRow(
        Appointment appointment,
        AppointmentRow row)
    {
        row.OwnerId = appointment.OwnerId.Value;
        row.PetId = appointment.PetId.Value;
        row.ClinicId = appointment.ClinicId.Value;
        row.VeterinarianId =
            appointment.VeterinarianId.Value;

        row.AppointmentDate = appointment.Slot.Date;
        row.StartTime = appointment.Slot.StartTime;
        row.EndTime = appointment.Slot.EndTime;

        row.Reason = appointment.Reason;
        row.Status = appointment.Status;

        row.ClinicalAttentionId =
            appointment.ClinicalAttentionId;

        row.CreatedAt = appointment.CreatedAt;
        row.UpdatedAt = appointment.UpdatedAt;
    }
}
