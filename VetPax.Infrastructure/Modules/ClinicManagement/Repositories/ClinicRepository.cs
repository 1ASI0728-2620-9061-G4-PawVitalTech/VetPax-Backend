
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Infrastructure.Modules.ClinicManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Repositories;

public sealed class ClinicRepository(VetPaxDbContext db)
    : IClinicRepository
{
    public async Task<Clinic?> FindByIdAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.Clinics
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.ClinicId == clinicId.Value,
                cancellationToken);

        if (row is null)
            return null;

        var administrators = await db.ClinicAdministrators
            .AsNoTracking()
            .Where(x => x.ClinicId == clinicId.Value)
            .Select(x => x.AdministratorId)
            .ToListAsync(cancellationToken);

        var hours = await db.ClinicOpeningHours
            .AsNoTracking()
            .Where(x => x.ClinicId == clinicId.Value)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.OpenTime)
            .ToListAsync(cancellationToken);

        return Clinic.Rehydrate(
            clinicId,
            new ClinicGeneralData(
                row.Name,
                row.Address,
                row.Phone,
                row.ContactEmail),
            new OpeningSchedule(hours.Select(x =>
                new DailyOpeningHours(
                    x.DayOfWeek,
                    x.OpenTime,
                    x.CloseTime))),
            administrators.Select(x =>
                new AdministratorId(x)),
            DateTime.SpecifyKind(
                row.UpdatedAt, DateTimeKind.Utc));
    }

    public Task<bool> ExistsByIdAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default)
    {
        return db.Clinics.AnyAsync(
            x => x.ClinicId == clinicId.Value,
            cancellationToken);
    }

    public async Task SaveAsync(
        Clinic clinic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clinic);

        var id = clinic.ClinicId.Value;

        var row = await db.Clinics.SingleOrDefaultAsync(
            x => x.ClinicId == id,
            cancellationToken);

        if (row is null)
        {
            row = new ClinicRow { ClinicId = id };
            db.Clinics.Add(row);
        }

        row.Name = clinic.GeneralData.Name;
        row.Address = clinic.GeneralData.Address;
        row.Phone = clinic.GeneralData.Phone;
        row.ContactEmail = clinic.GeneralData.ContactEmail;
        row.UpdatedAt = clinic.UpdatedAt;

        var existingAdmins = await db.ClinicAdministrators
            .Where(x => x.ClinicId == id)
            .ToListAsync(cancellationToken);

        var desiredAdmins = clinic.Administrators
            .Select(x => x.Value)
            .ToHashSet();

        var existingIds = existingAdmins
            .Select(x => x.AdministratorId)
            .ToHashSet();

        db.ClinicAdministrators.RemoveRange(
            existingAdmins.Where(x =>
                !desiredAdmins.Contains(x.AdministratorId)));

        foreach (var adminId in desiredAdmins.Except(existingIds))
        {
            db.ClinicAdministrators.Add(
                new ClinicAdministratorRow
                {
                    ClinicId = id,
                    AdministratorId = adminId
                });
        }

        var oldHours = await db.ClinicOpeningHours
            .Where(x => x.ClinicId == id)
            .ToListAsync(cancellationToken);

        db.ClinicOpeningHours.RemoveRange(oldHours);

        foreach (var hour in clinic.OpeningSchedule.Days)
        {
            db.ClinicOpeningHours.Add(
                new ClinicOpeningHourRow
                {
                    OpeningHoursId = Guid.NewGuid(),
                    ClinicId = id,
                    DayOfWeek = hour.DayOfWeek,
                    OpenTime = hour.OpenTime,
                    CloseTime = hour.CloseTime
                });
        }
    }
}
