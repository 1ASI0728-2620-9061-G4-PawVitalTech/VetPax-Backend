using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Adapters;

public sealed class AppointmentClinicAdapter
    : IAppointmentClinicPort
{
    private readonly VetPaxDbContext _db;

    public AppointmentClinicAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public Task<bool> IsActiveVeterinarianAsync(
        Guid veterinarianId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (veterinarianId == Guid.Empty ||
            clinicId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return _db.ClinicVeterinarians
            .AsNoTracking()
            .AnyAsync(
                x => x.VeterinarianId == veterinarianId &&
                     x.ClinicId == clinicId &&
                     x.IsActive,
                cancellationToken);
    }
    public async Task<IReadOnlyList<AppointmentOpeningInterval>>
        GetOpeningIntervalsAsync(
            Guid clinicId,
            DayOfWeek dayOfWeek,
            CancellationToken cancellationToken = default)
    {
        return await _db.ClinicOpeningHours
            .AsNoTracking()
            .Where(h =>
                h.ClinicId == clinicId &&
                h.DayOfWeek == dayOfWeek)
            .OrderBy(h => h.OpenTime)
            .Select(h => new AppointmentOpeningInterval(
                h.OpenTime,
                h.CloseTime))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> IsWithinOpeningHoursAsync(
        Guid clinicId,
        AppointmentSlot slot,
        CancellationToken cancellationToken = default)
    {
        if (clinicId == Guid.Empty || slot is null)
            return false;

        var dayOfWeek = slot.DayOfWeek;

        return await _db.ClinicOpeningHours
            .AsNoTracking()
            .AnyAsync(
                x => x.ClinicId == clinicId &&
                     x.DayOfWeek == dayOfWeek &&
                     x.OpenTime <= slot.StartTime &&
                     x.CloseTime >= slot.EndTime,
                cancellationToken);
    }
}