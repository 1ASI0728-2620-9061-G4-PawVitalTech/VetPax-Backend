
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Enums;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using VetPax.Domain.Shared.Events;
using VetPax.Infrastructure.Modules.NutritionManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Adapters;

public sealed class NutritionUnitOfWorkAdapter : INutritionUnitOfWork
{
    private readonly VetPaxDbContext _db;

    public NutritionUnitOfWorkAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class NutritionOutboxAdapter : INutritionOutboxPort
{
    private readonly VetPaxDbContext _db;

    public NutritionOutboxAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task StageAsync(
        IReadOnlyList<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var type = domainEvent.GetType();

            await _db.Set<FeedingOutboxRow>()
                .AddAsync(new FeedingOutboxRow
                {
                    EventId = Guid.NewGuid(),
                    EventType = type.FullName ?? type.Name,
                    Payload = JsonSerializer.Serialize(
                        domainEvent,
                        type),
                    OccurredAtUtc = domainEvent.OccurredAt,
                    AttemptCount = 0
                }, cancellationToken);
        }
    }
}

public sealed class NutritionReminderAdapter : INutritionReminderPort
{
    private readonly VetPaxDbContext _db;
    private readonly TimeProvider _clock;

    public NutritionReminderAdapter(
        VetPaxDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<bool> TryStageAsync(
        FeedingReminderOccurrence occurrence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        if (occurrence.PlanId == Guid.Empty ||
            occurrence.PetId == Guid.Empty ||
            occurrence.ScheduleId == Guid.Empty ||
            occurrence.PlanVersion < 1 ||
            occurrence.LocalDate == default ||
            occurrence.ScheduledAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Invalid feeding reminder occurrence.");
        }

        var tracked = _db.Set<FeedingReminderRow>()
            .Local
            .Any(x =>
                x.PlanId == occurrence.PlanId &&
                x.PlanVersion == occurrence.PlanVersion &&
                x.ScheduleId == occurrence.ScheduleId &&
                x.LocalDate == occurrence.LocalDate);

        if (tracked)
            return false;

        var exists = await _db.Set<FeedingReminderRow>()
            .AsNoTracking()
            .AnyAsync(
                x => x.PlanId == occurrence.PlanId &&
                     x.PlanVersion == occurrence.PlanVersion &&
                     x.ScheduleId == occurrence.ScheduleId &&
                     x.LocalDate == occurrence.LocalDate,
                cancellationToken);

        if (exists)
            return false;

        await _db.Set<FeedingReminderRow>()
            .AddAsync(new FeedingReminderRow
            {
                ReminderId = Guid.NewGuid(),
                PlanId = occurrence.PlanId,
                PetId = occurrence.PetId,
                ScheduleId = occurrence.ScheduleId,
                PlanVersion = occurrence.PlanVersion,
                LocalDate = occurrence.LocalDate,
                ScheduledAtUtc = occurrence.ScheduledAtUtc,
                Status = "Pending",
                CreatedAtUtc = _clock.GetUtcNow().UtcDateTime
            }, cancellationToken);

        return true;
    }

    
    public async Task CancelPendingForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        var cancelable = new[]
        {
            "Pending",
            "Retry",
            "Processing"
        };

        // Include rows newly staged in this DbContext.
        var localRows = _db.Set<FeedingReminderRow>()
            .Local
            .Where(x =>
                x.PlanId == planId &&
                cancelable.Contains(x.Status))
            .ToArray();

        var persistedRows = await _db.Set<FeedingReminderRow>()
            .Where(x =>
                x.PlanId == planId &&
                (x.Status == "Pending" ||
                 x.Status == "Retry" ||
                 x.Status == "Processing"))
            .ToListAsync(cancellationToken);

        foreach (var reminder in localRows.Concat(persistedRows)
                     .Distinct())
        {
            reminder.Status = "Cancelled";
            reminder.LeaseId = null;
            reminder.LockedUntilUtc = null;
            reminder.NextAttemptAtUtc = null;
            reminder.ProcessedAtUtc = DateTime.UtcNow;
        }
    }

}

public sealed class NutritionReminderCandidatesAdapter
    : INutritionReminderCandidatesPort
{
    private readonly VetPaxDbContext _db;
    private readonly IFeedingPlanRepository _repository;

    public NutritionReminderCandidatesAdapter(
        VetPaxDbContext db,
        IFeedingPlanRepository repository)
    {
        _db = db;
        _repository = repository;
    }

    public async Task<IReadOnlyList<FeedingPlan>> FindCandidatesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        var planIds = await _db.Set<FeedingPlanRow>()
            .AsNoTracking()
            .Where(x =>
                x.Status == FeedingPlanStatus.Active &&
                x.RemindersEnabled)
            .OrderBy(x => x.PlanId)
            .Select(x => x.PlanId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var result = new List<FeedingPlan>();

        foreach (var planId in planIds)
        {
            var plan = await _repository.FindByIdAsync(
                new FeedingPlanId(planId),
                cancellationToken);

            if (plan is not null)
                result.Add(plan);
        }

        return result;
    }
}
