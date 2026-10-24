
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Entities;
using VetPax.Domain.Modules.NutritionManagement.Enums;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using VetPax.Infrastructure.Modules.NutritionManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Repositories;

public sealed class FeedingPlanRepository : IFeedingPlanRepository
{
    private readonly VetPaxDbContext _db;

    public FeedingPlanRepository(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<FeedingPlan?> FindByIdAsync(
        FeedingPlanId planId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<FeedingPlanRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.PlanId == planId.Value,
                cancellationToken);

        if (row is null)
            return null;

        var schedules = await _db.Set<FeedingScheduleRow>()
            .AsNoTracking()
            .Where(x => x.PlanId == planId.Value)
            .ToListAsync(cancellationToken);

        var revisions = await _db.Set<FeedingPlanRevisionRow>()
            .AsNoTracking()
            .Where(x => x.PlanId == planId.Value)
            .OrderBy(x => x.Version)
            .ToListAsync(cancellationToken);

        var currentSchedules = schedules
            .Where(x => x.PlanVersion == row.Version)
            .Select(ToSchedule)
            .ToArray();

        var history = revisions.Select(revision =>
            new FeedingPlanRevision(
                revision.Version,
                revision.FoodName,
                revision.Recommendations,
                revision.StartDate,
                revision.EndDate,
                revision.TimeZoneId,
                schedules
                    .Where(x => x.PlanVersion == revision.Version)
                    .Select(ToSchedule),
                AsUtc(revision.ReplacedAtUtc)))
            .ToArray();

        return FeedingPlan.Rehydrate(
            new FeedingPlanId(row.PlanId),
            row.PetId,
            row.ClinicId,
            row.VeterinarianId,
            row.FoodName,
            row.Recommendations,
            row.StartDate,
            row.EndDate,
            row.TimeZoneId,
            currentSchedules,
            row.Status,
            row.RemindersEnabled,
            row.Version,
            history,
            AsUtc(row.CreatedAtUtc),
            AsUtc(row.UpdatedAtUtc));
    }

    public async Task<FeedingPlan?> FindActiveByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        var planId = await _db.Set<FeedingActivePlanRow>()
            .AsNoTracking()
            .Where(x => x.PetId == petId)
            .Select(x => (Guid?)x.PlanId)
            .SingleOrDefaultAsync(cancellationToken);

        return planId.HasValue
            ? await FindByIdAsync(
                new FeedingPlanId(planId.Value),
                cancellationToken)
            : null;
    }

    public async Task SaveAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var row = await _db.Set<FeedingPlanRow>()
            .SingleOrDefaultAsync(
                x => x.PlanId == plan.PlanId.Value,
                cancellationToken);

        if (row is null)
        {
            row = new FeedingPlanRow
            {
                PlanId = plan.PlanId.Value,
                PetId = plan.PetId,
                ClinicId = plan.ClinicId,
                VeterinarianId = plan.VeterinarianId,
                CreatedAtUtc = plan.CreatedAtUtc,
                ConcurrencyStamp = 1
            };

            ApplyCurrentState(row, plan);

            await _db.Set<FeedingPlanRow>()
                .AddAsync(row, cancellationToken);

            if (plan.Status == FeedingPlanStatus.Active)
            {
                await _db.Set<FeedingActivePlanRow>()
                    .AddAsync(new FeedingActivePlanRow
                    {
                        PetId = plan.PetId,
                        PlanId = plan.PlanId.Value
                    }, cancellationToken);
            }
        }
        else
        {
            if (row.PetId != plan.PetId ||
                row.ClinicId != plan.ClinicId ||
                row.VeterinarianId != plan.VeterinarianId)
            {
                throw new InvalidOperationException(
                    "Feeding plan ownership cannot be changed.");
            }

            ApplyCurrentState(row, plan);
            row.ConcurrencyStamp++;

            if (plan.Status == FeedingPlanStatus.Inactive)
            {
                var activeSlot = await _db
                    .Set<FeedingActivePlanRow>()
                    .SingleOrDefaultAsync(
                        x => x.PetId == plan.PetId &&
                             x.PlanId == plan.PlanId.Value,
                        cancellationToken);

                if (activeSlot is not null)
                {
                    _db.Set<FeedingActivePlanRow>()
                        .Remove(activeSlot);
                }
            }
        }

        await SynchronizeHistoryAsync(
            plan, cancellationToken);
    }

    private async Task SynchronizeHistoryAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken)
    {
        var existingRevisions = await _db
            .Set<FeedingPlanRevisionRow>()
            .Where(x => x.PlanId == plan.PlanId.Value)
            .Select(x => x.Version)
            .ToListAsync(cancellationToken);

        var revisionVersions = existingRevisions.ToHashSet();

        foreach (var revision in plan.Revisions)
        {
            if (revisionVersions.Add(revision.Version))
            {
                await _db.Set<FeedingPlanRevisionRow>()
                    .AddAsync(new FeedingPlanRevisionRow
                    {
                        PlanId = plan.PlanId.Value,
                        Version = revision.Version,
                        FoodName = revision.FoodName,
                        Recommendations = revision.Recommendations,
                        StartDate = revision.StartDate,
                        EndDate = revision.EndDate,
                        TimeZoneId = revision.TimeZoneId,
                        ReplacedAtUtc = revision.ReplacedAtUtc
                    }, cancellationToken);
            }
        }

        var existingSchedules = await _db
            .Set<FeedingScheduleRow>()
            .Where(x => x.PlanId == plan.PlanId.Value)
            .Select(x => new
            {
                x.PlanVersion,
                x.ScheduleId
            })
            .ToListAsync(cancellationToken);

        var keys = existingSchedules
            .Select(x => (x.PlanVersion, x.ScheduleId))
            .ToHashSet();

        foreach (var revision in plan.Revisions)
        {
            foreach (var schedule in revision.Schedules)
            {
                await StageScheduleAsync(
                    plan.PlanId.Value,
                    revision.Version,
                    schedule,
                    keys,
                    cancellationToken);
            }
        }

        foreach (var schedule in plan.Schedules)
        {
            await StageScheduleAsync(
                plan.PlanId.Value,
                plan.Version,
                schedule,
                keys,
                cancellationToken);
        }
    }

    private async Task StageScheduleAsync(
        Guid planId,
        int version,
        FeedingSchedule schedule,
        HashSet<(int, Guid)> keys,
        CancellationToken cancellationToken)
    {
        if (!keys.Add((version, schedule.ScheduleId)))
            return;

        await _db.Set<FeedingScheduleRow>()
            .AddAsync(new FeedingScheduleRow
            {
                PlanId = planId,
                PlanVersion = version,
                ScheduleId = schedule.ScheduleId,
                LocalTime = schedule.LocalTime,
                DaysOfWeek = string.Join(
                    ",",
                    schedule.DaysOfWeek.Select(x => (int)x)),
                Quantity = schedule.Portion.Quantity,
                Unit = schedule.Portion.Unit
            }, cancellationToken);
    }

    private static FeedingSchedule ToSchedule(
        FeedingScheduleRow row)
    {
        var days = row.DaysOfWeek
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries)
            .Select(x => (DayOfWeek)int.Parse(x))
            .ToArray();

        return new FeedingSchedule(
            row.ScheduleId,
            row.LocalTime,
            new FeedingPortion(
                row.Quantity,
                row.Unit),
            days);
    }

    private static void ApplyCurrentState(
        FeedingPlanRow row,
        FeedingPlan plan)
    {
        row.FoodName = plan.FoodName;
        row.Recommendations = plan.Recommendations;
        row.StartDate = plan.StartDate;
        row.EndDate = plan.EndDate;
        row.TimeZoneId = plan.TimeZoneId;

        row.Status = plan.Status;
        row.RemindersEnabled = plan.RemindersEnabled;
        row.Version = plan.Version;
        row.UpdatedAtUtc = plan.UpdatedAtUtc;
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }
}
