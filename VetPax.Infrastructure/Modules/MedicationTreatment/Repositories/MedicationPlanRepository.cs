
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Entities;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Repositories;

public sealed class MedicationPlanRepository
    : IMedicationPlanRepository
{
    private readonly VetPaxDbContext _db;

    public MedicationPlanRepository(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<MedicationPlan?> FindByIdAsync(
        MedicationPlanId planId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<MedicationPlanRow>()
            .SingleOrDefaultAsync(
                x => x.PlanId == planId.Value,
                cancellationToken);

        return row is null
            ? null
            : await RehydrateAsync(row, cancellationToken);
    }

    public async Task<MedicationPlan?> FindByDoseIdAsync(
        MedicationDoseId doseId,
        CancellationToken cancellationToken = default)
    {
        var dose = await _db.Set<MedicationDoseRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.DoseId == doseId.Value,
                cancellationToken);

        if (dose is null)
            return null;

        return await FindByIdAsync(
            new MedicationPlanId(dose.PlanId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<MedicationPlan>> ListByPetAsync(
        MedicationPetId petId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Set<MedicationPlanRow>()
            .Where(x => x.PetId == petId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<MedicationPlan>();

        foreach (var row in rows)
        {
            result.Add(await RehydrateAsync(
                row,
                cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<MedicationPlan>>
        FindEligibleForGenerationAsync(
            DateOnly throughDate,
            int limit,
            CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return Array.Empty<MedicationPlan>();

        var now = DateTime.UtcNow;

        // Selección de candidatos. No se limita a los
        // primeros N planes antes de comprobar si
        // necesitan dosis nuevas.
        var rows = await _db.Set<MedicationPlanRow>()
            .Where(x =>
                x.Status == MedicationPlanStatus.Active &&
                x.StartDate <= throughDate)
            .OrderBy(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<MedicationPlan>();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var zone = TimeZoneInfo.FindSystemTimeZoneById(
                row.TimeZoneId);

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(
                now,
                zone);

            var localToday = DateOnly.FromDateTime(localNow);

            var from = row.StartDate > localToday
                ? row.StartDate
                : localToday;

            var to = row.EndDate.HasValue &&
                     row.EndDate.Value < throughDate
                ? row.EndDate.Value
                : throughDate;

            if (from > to)
                continue;

            var schedules = await _db.Set<MedicationScheduleRow>()
                .AsNoTracking()
                .Where(x =>
                    x.PlanId == row.PlanId &&
                    x.ScheduleVersion == row.ScheduleVersion)
                .ToListAsync(cancellationToken);

            if (schedules.Count == 0)
                continue;

            var existing = await _db.Set<MedicationDoseRow>()
                .AsNoTracking()
                .Where(x =>
                    x.PlanId == row.PlanId &&
                    x.ScheduleVersion == row.ScheduleVersion &&
                    x.LocalDate >= from &&
                    x.LocalDate <= to)
                .Select(x => new
                {
                    x.ScheduleId,
                    x.LocalDate
                })
                .ToListAsync(cancellationToken);

            var existingKeys = existing
                .Select(x => (x.ScheduleId, x.LocalDate))
                .ToHashSet();

            var eligible = false;

            for (var date = from;
                 date <= to && !eligible;
                 date = date.AddDays(1))
            {
                foreach (var schedule in schedules)
                {
                    if ((schedule.DaysMask &
                         (1 << (int)date.DayOfWeek)) == 0)
                    {
                        continue;
                    }

                    if (existingKeys.Contains(
                            (schedule.ScheduleId, date)))
                    {
                        continue;
                    }

                    var local = DateTime.SpecifyKind(
                        date.ToDateTime(schedule.LocalTime),
                        DateTimeKind.Unspecified);

                    // Application es responsable de rechazar
                    // horarios ambiguos o inexistentes.
                    // No se descartan silenciosamente aquí.
                    if (zone.IsInvalidTime(local) ||
                        zone.IsAmbiguousTime(local))
                    {
                        eligible = true;
                        break;
                    }

                    var due = TimeZoneInfo.ConvertTimeToUtc(
                        local,
                        zone);

                    if (due > now)
                    {
                        eligible = true;
                        break;
                    }
                }
            }

            if (!eligible)
                continue;

            result.Add(await RehydrateAsync(
                row,
                cancellationToken));

            if (result.Count >= limit)
                break;
        }

        return result;
    }

    public async Task SaveAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var plans = _db.Set<MedicationPlanRow>();

        // Mantener el Row rastreado desde FindByIdAsync.
        // Su Version original es el token de concurrencia.
        var existing = plans.Local
            .SingleOrDefault(x => x.PlanId == plan.PlanId.Value);

        if (existing is null)
        {
            existing = await plans.SingleOrDefaultAsync(
                x => x.PlanId == plan.PlanId.Value,
                cancellationToken);
        }

        if (existing is null)
        {
            var row = new MedicationPlanRow
            {
                PlanId = plan.PlanId.Value,
                OwnerId = plan.OwnerId.Value,
                PetId = plan.PetId.Value,
                ClinicId = plan.ClinicId.Value,
                VeterinarianId = plan.VeterinarianId.Value,
                ClinicalAttentionId = plan.ClinicalAttentionId,
                MedicationName = plan.MedicationName,
                Route = plan.Route,
                Instructions = plan.Instructions,
                TimeZoneId = plan.TimeZoneId,
                StartDate = plan.StartDate,
                EndDate = plan.EndDate,
                Status = plan.Status,
                RemindersEnabled = plan.RemindersEnabled,
                ScheduleVersion = plan.ScheduleVersion,
                Version = 1,
                CreatedAtUtc = plan.CreatedAtUtc,
                UpdatedAtUtc = plan.UpdatedAtUtc
            };

            plans.Add(row);
        }
        else
        {
            existing.Status = plan.Status;
            existing.RemindersEnabled = plan.RemindersEnabled;
            existing.ScheduleVersion = plan.ScheduleVersion;
            existing.UpdatedAtUtc = plan.UpdatedAtUtc;

            // EF Core comparará el valor ORIGINAL de
            // Version en la condición del UPDATE.
            existing.Version++;
        }

        await SynchronizeSchedulesAsync(
            plan,
            cancellationToken);

        await SynchronizeDosesAsync(
            plan,
            cancellationToken);

        await SynchronizePreferenceAsync(
            plan,
            cancellationToken);

        // Sin SaveChangesAsync aquí.
        // MedicationCommitService llama al UnitOfWork.
    }

    private async Task SynchronizeSchedulesAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Set<MedicationScheduleRow>()
            .Where(x => x.PlanId == plan.PlanId.Value)
            .ToListAsync(cancellationToken);

        var known = rows
            .Select(x => x.ScheduleId)
            .ToHashSet();

        foreach (var schedule in plan.Schedules)
        {
            if (known.Contains(schedule.ScheduleId.Value))
                continue;

            var daysMask = 0;

            foreach (var day in schedule.DaysOfWeek)
                daysMask |= 1 << (int)day;

            _db.Set<MedicationScheduleRow>().Add(
                new MedicationScheduleRow
                {
                    ScheduleId = schedule.ScheduleId.Value,
                    PlanId = plan.PlanId.Value,
                    ScheduleVersion = plan.ScheduleVersion,
                    LocalTime = schedule.LocalTime,
                    DaysMask = daysMask,
                    Amount = schedule.Dosage.Amount,
                    Unit = schedule.Dosage.Unit
                });
        }

        // Los horarios antiguos NO se eliminan:
        // las dosis históricas conservan su referencia.
    }

    private async Task SynchronizeDosesAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken)
    {
        var rows = await _db.Set<MedicationDoseRow>()
            .Where(x => x.PlanId == plan.PlanId.Value)
            .ToListAsync(cancellationToken);

        var byId = rows.ToDictionary(x => x.DoseId);

        var administrationRows =
            await (
                from administration in
                    _db.Set<MedicationAdministrationRow>()
                join dose in _db.Set<MedicationDoseRow>()
                    on administration.DoseId equals dose.DoseId
                where dose.PlanId == plan.PlanId.Value
                select administration
            ).ToListAsync(cancellationToken);

        var administrationIds = administrationRows
            .Select(x => x.DoseId)
            .ToHashSet();

        foreach (var dose in plan.Doses)
        {
            if (byId.TryGetValue(
                    dose.DoseId.Value,
                    out var existing))
            {
                existing.Status = dose.Status;
            }
            else
            {
                _db.Set<MedicationDoseRow>().Add(
                    new MedicationDoseRow
                    {
                        DoseId = dose.DoseId.Value,
                        PlanId = plan.PlanId.Value,
                        ScheduleId = dose.ScheduleId.Value,
                        ScheduleVersion = dose.ScheduleVersion,
                        LocalDate = dose.LocalDate,
                        LocalTime = dose.LocalTime,
                        ScheduledAtUtc = dose.ScheduledAtUtc,
                        Status = dose.Status
                    });
            }

            if (dose.Administration is not { } administration ||
                administrationIds.Contains(dose.DoseId.Value))
            {
                continue;
            }

            _db.Set<MedicationAdministrationRow>().Add(
                new MedicationAdministrationRow
                {
                    AdministrationId =
                        administration.AdministrationId.Value,
                    DoseId = dose.DoseId.Value,
                    AdministeredBy =
                        administration.AdministeredBy.Value,
                    AdministeredAtUtc =
                        administration.AdministeredAtUtc,
                    RecordedAtUtc =
                        administration.RecordedAtUtc
                });
        }
    }

    private async Task SynchronizePreferenceAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken)
    {
        var preferences =
            _db.Set<MedicationReminderPreferenceRow>();

        var row = await preferences.SingleOrDefaultAsync(
            x => x.PlanId == plan.PlanId.Value,
            cancellationToken);

        if (row is null)
        {
            preferences.Add(
                new MedicationReminderPreferenceRow
                {
                    PlanId = plan.PlanId.Value,
                    Enabled = plan.RemindersEnabled,
                    UpdatedAtUtc = plan.UpdatedAtUtc
                });
        }
        else
        {
            row.Enabled = plan.RemindersEnabled;
            row.UpdatedAtUtc = plan.UpdatedAtUtc;
        }
    }

    private async Task<MedicationPlan> RehydrateAsync(
        MedicationPlanRow row,
        CancellationToken cancellationToken)
    {
        var schedules = await _db.Set<MedicationScheduleRow>()
            .AsNoTracking()
            .Where(x =>
                x.PlanId == row.PlanId &&
                x.ScheduleVersion == row.ScheduleVersion)
            .ToListAsync(cancellationToken);

        var doses = await _db.Set<MedicationDoseRow>()
            .AsNoTracking()
            .Where(x => x.PlanId == row.PlanId)
            .ToListAsync(cancellationToken);

        var administrations =
            await (
                from administration in
                    _db.Set<MedicationAdministrationRow>()
                join dose in _db.Set<MedicationDoseRow>()
                    on administration.DoseId equals dose.DoseId
                where dose.PlanId == row.PlanId
                select administration
            ).AsNoTracking()
             .ToListAsync(cancellationToken);

        var byDoseId = administrations
            .ToDictionary(x => x.DoseId);

        var domainSchedules = schedules.Select(x =>
        {
            var days = Enum.GetValues<DayOfWeek>()
                .Where(day =>
                    (x.DaysMask & (1 << (int)day)) != 0)
                .ToArray();

            return new MedicationSchedule(
                new MedicationScheduleId(x.ScheduleId),
                x.LocalTime,
                days,
                new MedicationDosage(
                    x.Amount,
                    x.Unit));
        }).ToArray();

        var domainDoses = doses.Select(x =>
        {
            DoseAdministration? administration = null;

            if (byDoseId.TryGetValue(
                    x.DoseId,
                    out var administrationRow))
            {
                administration = new DoseAdministration(
                    new MedicationAdministrationId(
                        administrationRow.AdministrationId),
                    new MedicationDoseId(x.DoseId),
                    new MedicationOwnerId(
                        administrationRow.AdministeredBy),
                    AsUtc(administrationRow.AdministeredAtUtc),
                    AsUtc(administrationRow.RecordedAtUtc));
            }

            return MedicationDose.Rehydrate(
                new MedicationDoseId(x.DoseId),
                new MedicationPlanId(x.PlanId),
                new MedicationScheduleId(x.ScheduleId),
                x.ScheduleVersion,
                x.LocalDate,
                x.LocalTime,
                AsUtc(x.ScheduledAtUtc),
                x.Status,
                administration);
        }).ToArray();

        return MedicationPlan.Rehydrate(
            new MedicationPlanId(row.PlanId),
            new MedicationOwnerId(row.OwnerId),
            new MedicationPetId(row.PetId),
            new MedicationClinicId(row.ClinicId),
            new MedicationVeterinarianId(row.VeterinarianId),
            row.ClinicalAttentionId,
            row.MedicationName,
            row.Route,
            row.Instructions,
            row.StartDate,
            row.EndDate,
            row.TimeZoneId,
            row.Status,
            row.RemindersEnabled,
            row.ScheduleVersion,
            AsUtc(row.CreatedAtUtc),
            AsUtc(row.UpdatedAtUtc),
            domainSchedules,
            domainDoses);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }
}
