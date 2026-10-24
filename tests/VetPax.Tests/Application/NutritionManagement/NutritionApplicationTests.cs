
using NSubstitute;
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Application.Modules.NutritionManagement.Services;
using VetPax.Application.Modules.NutritionManagement.UseCases;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Events;
using VetPax.Domain.Modules.NutritionManagement.Repositories;
using VetPax.Domain.Modules.NutritionManagement.ValueObjects;
using VetPax.Domain.Shared.Events;
using Xunit;

namespace VetPax.Tests.Application.NutritionManagement;

public sealed class NutritionApplicationTests
{
    private readonly IFeedingPlanRepository _repository =
        Substitute.For<IFeedingPlanRepository>();

    private readonly INutritionIdentityPort _identity =
        Substitute.For<INutritionIdentityPort>();

    private readonly INutritionPatientPort _patients =
        Substitute.For<INutritionPatientPort>();

    private readonly INutritionVeterinarianPort _veterinarians =
        Substitute.For<INutritionVeterinarianPort>();

    private readonly INutritionReminderPort _reminders =
        Substitute.For<INutritionReminderPort>();

    private readonly INutritionReminderCandidatesPort _candidates =
        Substitute.For<INutritionReminderCandidatesPort>();

    private readonly INutritionOutboxPort _outbox =
        Substitute.For<INutritionOutboxPort>();

    private readonly INutritionUnitOfWork _unitOfWork =
        Substitute.For<INutritionUnitOfWork>();

    private readonly TimeProvider _clock = new FixedClock();

    private readonly NutritionAuthorizationService _authorization;
    private readonly NutritionScheduleFactory _scheduleFactory = new();
    private readonly NutritionTimeService _timeService = new();
    private readonly NutritionCommitService _commit;

    private static readonly DateOnly StartDate =
        new(2026, 11, 2);

    private static readonly DateOnly EndDate =
        new(2026, 11, 9);

    private static readonly DateTime PlanCreatedAt =
        new(2026, 10, 23, 19, 0, 0, DateTimeKind.Utc);

    public NutritionApplicationTests()
    {
        _authorization = new NutritionAuthorizationService(
            _identity,
            _patients,
            _veterinarians);

        _commit = new NutritionCommitService(
            _repository,
            _outbox,
            _unitOfWork);

        _repository.SaveAsync(
                Arg.Any<FeedingPlan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _outbox.StageAsync(
                Arg.Any<IReadOnlyList<IDomainEvent>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _unitOfWork.CommitAsync(
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Create_AuthorizedVeterinarian_SavesPlan()
    {
        var veterinarianId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();

        SetActor(veterinarianId, NutritionUserRole.Veterinarian);
        AllowVeterinarian();

        _repository.FindActiveByPetIdAsync(
                petId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FeedingPlan?>(null));

        var handler = new CreateFeedingPlanHandler(
            _repository,
            _authorization,
            _scheduleFactory,
            _timeService,
            _commit,
            _clock);

        var result = await handler.HandleAsync(
            CreateCommand(petId, clinicId));

        Assert.Equal(veterinarianId, result.VeterinarianId);
        Assert.Equal(petId, result.PetId);
        Assert.Equal("Active", result.Status);
        Assert.Equal(1, result.Version);

        await _repository.Received(1).SaveAsync(
            Arg.Any<FeedingPlan>(),
            Arg.Any<CancellationToken>());

        await _outbox.Received(1).StageAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(
                events => events.Any(
                    e => e is FeedingPlanPrescribedEvent)),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_OwnerCannotPrescribe()
    {
        SetActor(Guid.NewGuid(), NutritionUserRole.Owner);

        var handler = new CreateFeedingPlanHandler(
            _repository,
            _authorization,
            _scheduleFactory,
            _timeService,
            _commit,
            _clock);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(
                CreateCommand(Guid.NewGuid(), Guid.NewGuid())));

        await _repository.DidNotReceive().SaveAsync(
            Arg.Any<FeedingPlan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_RejectsExistingActivePlan()
    {
        var existing = CreatePlan();

        SetActor(
            existing.VeterinarianId,
            NutritionUserRole.Veterinarian);

        AllowVeterinarian();

        _repository.FindActiveByPetIdAsync(
                existing.PetId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FeedingPlan?>(existing));

        var handler = new CreateFeedingPlanHandler(
            _repository,
            _authorization,
            _scheduleFactory,
            _timeService,
            _commit,
            _clock);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                CreateCommand(
                    existing.PetId,
                    existing.ClinicId)));

        await _unitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetActive_OwnerCanReadOwnPlan()
    {
        var plan = CreatePlan();
        var ownerId = Guid.NewGuid();

        SetActor(ownerId, NutritionUserRole.Owner);
        SetPetOwner(ownerId);

        _repository.FindActiveByPetIdAsync(
                plan.PetId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FeedingPlan?>(plan));

        var handler = new GetActiveFeedingPlanHandler(
            _repository,
            _authorization);

        var result = await handler.HandleAsync(
            new GetActiveFeedingPlanQuery(plan.PetId));

        Assert.Equal(plan.PlanId.Value, result.PlanId);
        Assert.Equal("Veterinary food", result.FoodName);
        Assert.Single(result.Schedules);
    }

    [Fact]
    public async Task GetActive_OtherOwnerIsRejected()
    {
        var plan = CreatePlan();

        SetActor(Guid.NewGuid(), NutritionUserRole.Owner);
        SetPetOwner(Guid.NewGuid());

        _repository.FindActiveByPetIdAsync(
                plan.PetId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FeedingPlan?>(plan));

        var handler = new GetActiveFeedingPlanHandler(
            _repository,
            _authorization);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(
                new GetActiveFeedingPlanQuery(plan.PetId)));
    }

    [Fact]
    public async Task Update_CreatesRevisionAndCancelsOldReminders()
    {
        var plan = CreatePlan();

        SetActor(
            plan.VeterinarianId,
            NutritionUserRole.Veterinarian);

        AllowVeterinarian();
        SetPlan(plan);

        _reminders.CancelPendingForPlanAsync(
                plan.PlanId.Value,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new UpdateFeedingPlanHandler(
            _repository,
            _authorization,
            _scheduleFactory,
            _timeService,
            _reminders,
            _commit,
            _clock);

        var result = await handler.HandleAsync(
            new UpdateFeedingPlanCommand(
                plan.PlanId.Value,
                "Updated food",
                "Updated instructions",
                StartDate,
                EndDate,
                "America/Lima",
                ScheduleInputs()));

        Assert.Equal(2, result.Version);
        Assert.Equal("Updated food", result.FoodName);

        var previous = Assert.Single(result.Revisions);
        Assert.Equal("Veterinary food", previous.FoodName);

        await _reminders.Received(1)
            .CancelPendingForPlanAsync(
                plan.PlanId.Value,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableReminders_CancelsPendingOccurrences()
    {
        var plan = CreatePlan();
        var ownerId = Guid.NewGuid();

        plan.ConfigureReminders(
            true,
            PlanCreatedAt.AddMinutes(1));

        plan.PullDomainEvents();

        SetActor(ownerId, NutritionUserRole.Owner);
        SetPetOwner(ownerId);
        SetPlan(plan);

        _reminders.CancelPendingForPlanAsync(
                plan.PlanId.Value,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new ConfigureFeedingRemindersHandler(
            _repository,
            _authorization,
            _reminders,
            _commit,
            _clock);

        var result = await handler.HandleAsync(
            new ConfigureFeedingRemindersCommand(
                plan.PlanId.Value,
                false));

        Assert.False(result.RemindersEnabled);

        await _reminders.Received(1)
            .CancelPendingForPlanAsync(
                plan.PlanId.Value,
                Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateReminder_StagesCorrectUtcOccurrence()
    {
        var plan = CreatePlan();
        var schedule = Assert.Single(plan.Schedules);

        plan.ConfigureReminders(
            true,
            PlanCreatedAt.AddMinutes(1));

        plan.PullDomainEvents();

        _candidates.FindCandidatesAsync(
                20,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FeedingPlan>>(
                new[] { plan }));

        _reminders.TryStageAsync(
                Arg.Any<FeedingReminderOccurrence>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var handler = new GenerateFeedingRemindersHandler(
            _candidates,
            _reminders,
            _timeService,
            _commit,
            _clock);

        var count = await handler.HandleAsync(
            new GenerateFeedingRemindersCommand(
                StartDate,
                StartDate,
                20));

        Assert.Equal(1, count);

        await _reminders.Received(1).TryStageAsync(
            Arg.Is<FeedingReminderOccurrence>(x =>
                x.PlanId == plan.PlanId.Value &&
                x.ScheduleId == schedule.ScheduleId &&
                x.LocalDate == StartDate &&
                x.ScheduledAtUtc ==
                    new DateTime(
                        2026, 11, 2, 13, 0, 0,
                        DateTimeKind.Utc)),
            Arg.Any<CancellationToken>());

        await _outbox.Received(1).StageAsync(
            Arg.Is<IReadOnlyList<IDomainEvent>>(
                events => events.Any(
                    e => e is FeedingReminderGeneratedEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateReminder_DoesNotCommit()
    {
        var plan = CreatePlan();

        plan.ConfigureReminders(
            true,
            PlanCreatedAt.AddMinutes(1));

        plan.PullDomainEvents();

        _candidates.FindCandidatesAsync(
                20,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FeedingPlan>>(
                new[] { plan }));

        _reminders.TryStageAsync(
                Arg.Any<FeedingReminderOccurrence>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = new GenerateFeedingRemindersHandler(
            _candidates,
            _reminders,
            _timeService,
            _commit,
            _clock);

        var result = await handler.HandleAsync(
            new GenerateFeedingRemindersCommand(
                StartDate,
                StartDate));

        Assert.Equal(0, result);

        await _unitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    private void SetActor(
        Guid userId,
        NutritionUserRole role)
    {
        _identity.GetCurrentAsync(
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NutritionActor?>(
                new NutritionActor(userId, role)));
    }

    private void SetPetOwner(Guid ownerId)
    {
        _patients.GetOwnerIdAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Guid?>(ownerId));
    }

    private void AllowVeterinarian()
    {
        SetPetOwner(Guid.NewGuid());

        _veterinarians.CanPrescribeAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
    }

    private void SetPlan(FeedingPlan plan)
    {
        _repository.FindByIdAsync(
                new FeedingPlanId(plan.PlanId.Value),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FeedingPlan?>(plan));
    }

    private static CreateFeedingPlanCommand CreateCommand(
        Guid petId,
        Guid clinicId)
    {
        return new CreateFeedingPlanCommand(
            petId,
            clinicId,
            "Veterinary food",
            "Test instructions",
            StartDate,
            EndDate,
            "America/Lima",
            ScheduleInputs());
    }

    private static FeedingScheduleInput[] ScheduleInputs()
    {
        return new[]
        {
            new FeedingScheduleInput(
                new TimeOnly(8, 0),
                Enum.GetValues<DayOfWeek>(),
                150m,
                "g")
        };
    }

    private static FeedingPlan CreatePlan()
    {
        var plan = FeedingPlan.Prescribe(
            FeedingPlanId.New(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Veterinary food",
            "Original instructions",
            StartDate,
            EndDate,
            "America/Lima",
            new[]
            {
                new FeedingSchedule(
                    Guid.NewGuid(),
                    new TimeOnly(8, 0),
                    new FeedingPortion(150m, "g"),
                    Enum.GetValues<DayOfWeek>())
            },
            PlanCreatedAt);

        plan.PullDomainEvents();

        return plan;
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(
                2026, 10, 23, 20, 0, 0,
                TimeSpan.Zero);
        }
    }
}
