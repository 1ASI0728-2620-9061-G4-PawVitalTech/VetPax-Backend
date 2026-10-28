
using VetPax.Domain.Modules.NutritionManagement.Entities;
using VetPax.Domain.Modules.NutritionManagement.Enums;
using VetPax.Domain.Modules.NutritionManagement.Events;
using VetPax.Domain.Modules.NutritionManagement.Exceptions;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.NutritionManagement.Aggregates;

public sealed class FeedingPlan
{
    private readonly List<FeedingSchedule> _schedules = new();
    private readonly List<FeedingPlanRevision> _revisions = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public FeedingPlanId PlanId { get; }
    public Guid PetId { get; }
    public Guid ClinicId { get; }
    public Guid VeterinarianId { get; }

    public string FoodName { get; private set; } = string.Empty;
    public string? Recommendations { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string TimeZoneId { get; private set; } = string.Empty;

    public FeedingPlanStatus Status { get; private set; }
    public bool RemindersEnabled { get; private set; }
    public int Version { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<FeedingSchedule> Schedules =>
        _schedules.AsReadOnly();

    public IReadOnlyList<FeedingPlanRevision> Revisions =>
        _revisions.AsReadOnly();

    private FeedingPlan(
        FeedingPlanId planId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        string foodName,
        string? recommendations,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules,
        DateTime createdAtUtc)
    {
        if (planId.Value == Guid.Empty ||
            petId == Guid.Empty ||
            clinicId == Guid.Empty ||
            veterinarianId == Guid.Empty)
        {
            throw new NutritionDomainException(
                "Valid plan, pet, clinic and veterinarian IDs are required.");
        }

        ValidateUtc(createdAtUtc);

        PlanId = planId;
        PetId = petId;
        ClinicId = clinicId;
        VeterinarianId = veterinarianId;

        ApplyDetails(
            foodName,
            recommendations,
            startDate,
            endDate,
            timeZoneId,
            schedules);

        Status = FeedingPlanStatus.Active;
        RemindersEnabled = false;
        Version = 1;

        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    // ---------------------------------------
    // US08: prescribe a feeding plan
    // ---------------------------------------

    public static FeedingPlan Prescribe(
        FeedingPlanId planId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        string foodName,
        string? recommendations,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules,
        DateTime occurredAtUtc)
    {
        var plan = new FeedingPlan(
            planId,
            petId,
            clinicId,
            veterinarianId,
            foodName,
            recommendations,
            startDate,
            endDate,
            timeZoneId,
            schedules,
            occurredAtUtc);

        plan._domainEvents.Add(
            new FeedingPlanPrescribedEvent(
                plan.PlanId,
                plan.PetId,
                plan.ClinicId,
                plan.VeterinarianId,
                occurredAtUtc));

        return plan;
    }

    // ---------------------------------------
    // US08: update with full history
    // ---------------------------------------

    public void Update(
        string foodName,
        string? recommendations,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules,
        DateTime occurredAtUtc)
    {
        EnsureActive();
        ValidateUtc(occurredAtUtc);

        if (occurredAtUtc < UpdatedAtUtc)
            throw new NutritionDomainException(
                "Update timestamp cannot precede the last update.");

        // Validate the new information before
        // modifying any aggregate state.
        ValidateDetails(
            foodName,
            startDate,
            endDate,
            timeZoneId,
            schedules);

        var previousVersion = Version;

        var revision = new FeedingPlanRevision(
            Version,
            FoodName,
            Recommendations,
            StartDate,
            EndDate,
            TimeZoneId,
            _schedules,
            occurredAtUtc);

        ApplyDetails(
            foodName,
            recommendations,
            startDate,
            endDate,
            timeZoneId,
            schedules);

        _revisions.Add(revision);

        Version++;
        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new FeedingPlanUpdatedEvent(
                PlanId,
                PetId,
                previousVersion,
                Version,
                occurredAtUtc));
    }

    // ---------------------------------------
    // US09: configure reminders
    // ---------------------------------------

    public void ConfigureReminders(
        bool enabled,
        DateTime occurredAtUtc)
    {
        EnsureActive();
        ValidateUtc(occurredAtUtc);

        if (occurredAtUtc < UpdatedAtUtc)
            throw new NutritionDomainException(
                "Reminder timestamp cannot precede the last update.");

        if (RemindersEnabled == enabled)
            return;

        RemindersEnabled = enabled;
        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new FeedingRemindersConfiguredEvent(
                PlanId,
                PetId,
                enabled,
                occurredAtUtc));
    }

    // ---------------------------------------
    // Stop a feeding plan
    // ---------------------------------------

    public void Deactivate(DateTime occurredAtUtc)
    {
        EnsureActive();
        ValidateUtc(occurredAtUtc);

        if (occurredAtUtc < UpdatedAtUtc)
            throw new NutritionDomainException(
                "Deactivation timestamp cannot precede the last update.");

        Status = FeedingPlanStatus.Inactive;
        RemindersEnabled = false;

        Version++;
        UpdatedAtUtc = occurredAtUtc;

        _domainEvents.Add(
            new FeedingPlanDeactivatedEvent(
                PlanId,
                PetId,
                occurredAtUtc));
    }

    // ---------------------------------------
    // US09: check scheduled reminder
    // ---------------------------------------

    public bool CanGenerateReminder(
        Guid scheduleId,
        DateOnly localDate)
    {
        if (Status != FeedingPlanStatus.Active ||
            !RemindersEnabled ||
            localDate == default ||
            localDate < StartDate ||
            (EndDate.HasValue && localDate > EndDate.Value))
        {
            return false;
        }

        return _schedules.Any(schedule =>
            schedule.ScheduleId == scheduleId &&
            schedule.AppliesOn(localDate));
    }

    public void RecordReminderGenerated(
        Guid scheduleId,
        DateOnly localDate,
        DateTime occurredAtUtc)
    {
        ValidateUtc(occurredAtUtc);

        if (!CanGenerateReminder(scheduleId, localDate))
        {
            throw new NutritionDomainException(
                "A reminder cannot be generated for this plan and date.");
        }

        _domainEvents.Add(
            new FeedingReminderGeneratedEvent(
                PlanId,
                PetId,
                scheduleId,
                Version,
                localDate,
                occurredAtUtc));
    }

    // ---------------------------------------
    // Restore aggregate from persistence
    // without emitting domain events
    // ---------------------------------------

    public static FeedingPlan Rehydrate(
        FeedingPlanId planId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        string foodName,
        string? recommendations,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules,
        FeedingPlanStatus status,
        bool remindersEnabled,
        int version,
        IEnumerable<FeedingPlanRevision> revisions,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(revisions);

        ValidateUtc(updatedAtUtc);

        if (version < 1 ||
            !Enum.IsDefined(status) ||
            updatedAtUtc < createdAtUtc ||
            (status == FeedingPlanStatus.Inactive &&
             remindersEnabled))
        {
            throw new NutritionDomainException(
                "Invalid persisted feeding plan state.");
        }

        var plan = new FeedingPlan(
            planId,
            petId,
            clinicId,
            veterinarianId,
            foodName,
            recommendations,
            startDate,
            endDate,
            timeZoneId,
            schedules,
            createdAtUtc);

        var history = revisions.ToArray();

        if (history.Any(revision =>
                revision.Version < 1 ||
                revision.Version >= version) ||
            history.Select(revision => revision.Version)
                .Distinct().Count() != history.Length)
        {
            throw new NutritionDomainException(
                "Invalid feeding plan revision history.");
        }

        plan._revisions.AddRange(
            history.OrderBy(revision => revision.Version));

        plan.Status = status;
        plan.RemindersEnabled = remindersEnabled;
        plan.Version = version;
        plan.UpdatedAtUtc = updatedAtUtc;

        return plan;
    }

    // ---------------------------------------
    // Domain events
    // ---------------------------------------

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }

    // ---------------------------------------
    // Business rule validation
    // ---------------------------------------

    private void EnsureActive()
    {
        if (Status != FeedingPlanStatus.Active)
            throw new NutritionDomainException(
                "Only active feeding plans can be modified.");
    }

    private void ApplyDetails(
        string foodName,
        string? recommendations,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules)
    {
        var validatedSchedules = ValidateDetails(
            foodName,
            startDate,
            endDate,
            timeZoneId,
            schedules);

        FoodName = foodName.Trim();
        Recommendations = string.IsNullOrWhiteSpace(recommendations)
            ? null
            : recommendations.Trim();

        StartDate = startDate;
        EndDate = endDate;
        TimeZoneId = timeZoneId.Trim();

        _schedules.Clear();
        _schedules.AddRange(validatedSchedules);
    }

    private static FeedingSchedule[] ValidateDetails(
        string foodName,
        DateOnly startDate,
        DateOnly? endDate,
        string timeZoneId,
        IEnumerable<FeedingSchedule> schedules)
    {
        if (string.IsNullOrWhiteSpace(foodName))
            throw new NutritionDomainException(
                "Food name is required.");

        if (foodName.Trim().Length > 200)
            throw new NutritionDomainException(
                "Food name cannot exceed 200 characters.");

        if (startDate == default)
            throw new NutritionDomainException(
                "Feeding plan start date is required.");

        if (endDate.HasValue && endDate.Value < startDate)
            throw new NutritionDomainException(
                "End date cannot precede the start date.");

        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new NutritionDomainException(
                "A time zone is required.");

        if (timeZoneId.Trim().Length > 100)
            throw new NutritionDomainException(
                "Time zone cannot exceed 100 characters.");

        if (schedules is null)
            throw new NutritionDomainException(
                "Feeding schedules are required.");

        var result = schedules.ToArray();

        if (result.Length == 0)
            throw new NutritionDomainException(
                "At least one feeding schedule is required.");

        if (result.Any(schedule => schedule is null))
            throw new NutritionDomainException(
                "Feeding schedules cannot be null.");

        if (result.Select(schedule => schedule.ScheduleId)
                .Distinct().Count() != result.Length)
        {
            throw new NutritionDomainException(
                "Feeding schedule IDs must be unique.");
        }

        // Two portions cannot be prescribed at the
        // same local time on overlapping weekdays.
        for (var i = 0; i < result.Length; i++)
        {
            for (var j = i + 1; j < result.Length; j++)
            {
                if (result[i].LocalTime == result[j].LocalTime &&
                    result[i].DaysOfWeek
                        .Intersect(result[j].DaysOfWeek)
                        .Any())
                {
                    throw new NutritionDomainException(
                        "Feeding schedules cannot overlap.");
                }
            }
        }

        return result;
    }

    private static void ValidateUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
            throw new NutritionDomainException(
                "Domain timestamps must use UTC.");
    }
}
