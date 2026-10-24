
using NSubstitute;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.MedicationTreatment.Exceptions;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Application.Modules.MedicationTreatment.UseCases.ConfigureMedicationReminders;
using VetPax.Application.Modules.MedicationTreatment.UseCases.CreateMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GenerateMedicationDoses;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationDoses;
using VetPax.Application.Modules.MedicationTreatment.UseCases.RegisterDoseAdministration;
using VetPax.Application.Modules.MedicationTreatment.UseCases.UpdateMedicationPlan;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Enums;
using VetPax.Domain.Modules.MedicationTreatment.Events;
using VetPax.Domain.Modules.MedicationTreatment.Exceptions;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;
using VetPax.Domain.Shared.Events;
using Xunit;

namespace VetPax.Tests.Application.MedicationTreatment;

public sealed class MedicationApplicationTests
{
    private static readonly DateTime FixedNow =
        new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly Today =
        new(2026, 10, 23);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(FixedNow);
        }
    }

    private sealed class TestFixture
    {
        public Guid ActorId { get; }
        public Guid OwnerId { get; }
        public Guid PetId { get; }
        public Guid ClinicId { get; }
        public Guid VeterinarianId { get; }

        public IAuthenticatedUserPort CurrentUser { get; }
        public IMedicationPatientPort Patients { get; }
        public IMedicationVeterinarianPort Veterinarians { get; }
        public IMedicationClinicalContextPort ClinicalContext { get; }
        public IMedicationPlanRepository Repository { get; }
        public IMedicationReminderPort Reminders { get; }
        public IMedicationOutboxPort Outbox { get; }
        public IUnitOfWork UnitOfWork { get; }

        public MedicationAuthorizationService Authorization { get; }
        public MedicationCommitService Commit { get; }
        public MedicationTimeService Time { get; }
        public TimeProvider Clock { get; }

        public TestFixture(
            UserRole role,
            bool useAnotherOwner = false)
        {
            OwnerId = Guid.NewGuid();
            PetId = Guid.NewGuid();
            ClinicId = Guid.NewGuid();
            VeterinarianId = Guid.NewGuid();

            ActorId = role == UserRole.Veterinarian
                ? VeterinarianId
                : useAnotherOwner
                    ? Guid.NewGuid()
                    : OwnerId;

            CurrentUser = Substitute.For<IAuthenticatedUserPort>();
            Patients = Substitute.For<IMedicationPatientPort>();
            Veterinarians =
                Substitute.For<IMedicationVeterinarianPort>();

            ClinicalContext =
                Substitute.For<IMedicationClinicalContextPort>();

            Repository = Substitute.For<IMedicationPlanRepository>();
            Reminders = Substitute.For<IMedicationReminderPort>();
            Outbox = Substitute.For<IMedicationOutboxPort>();
            UnitOfWork = Substitute.For<IUnitOfWork>();

            Clock = new FixedTimeProvider();
            Time = new MedicationTimeService();

            CurrentUser.GetCurrentAsync(
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<AuthenticatedIdentity?>(
                    new AuthenticatedIdentity(
                        new UserId(ActorId),
                        "medication@test.local",
                        role)));

            Patients.GetOwnerIdAsync(
                    PetId,
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Guid?>(OwnerId));

            Veterinarians.CanPrescribeAsync(
                    VeterinarianId,
                    ClinicId,
                    PetId,
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            Authorization = new MedicationAuthorizationService(
                CurrentUser,
                Patients,
                Veterinarians);

            Commit = new MedicationCommitService(
                Repository,
                Outbox,
                UnitOfWork);
        }

        public MedicationPlan CreatePlan(
            bool withSchedules = true)
        {
            var schedules = withSchedules
                ? new[]
                {
                    new MedicationSchedule(
                        MedicationScheduleId.New(),
                        new TimeOnly(8, 0),
                        Enum.GetValues<DayOfWeek>(),
                        new MedicationDosage(1, "tablet"))
                }
                : Array.Empty<MedicationSchedule>();

            var plan = MedicationPlan.Prescribe(
                new MedicationOwnerId(OwnerId),
                new MedicationPetId(PetId),
                new MedicationClinicId(ClinicId),
                new MedicationVeterinarianId(VeterinarianId),
                "Medication test",
                "Oral",
                "Follow prescription",
                Today,
                Today.AddDays(7),
                "UTC",
                schedules,
                FixedNow);

            // El fixture representa un plan que ya existe
            // en la base de datos. Descartamos el evento
            // inicial para no mezclarlo con la prueba actual.
            plan.PullDomainEvents();

            return plan;
        }

        public void ReturnPlan(MedicationPlan plan)
        {
            Repository.FindByIdAsync(
                    plan.PlanId,
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<MedicationPlan?>(plan));
        }

        public void ReturnDose(
            MedicationDoseId doseId,
            MedicationPlan plan)
        {
            Repository.FindByDoseIdAsync(
                    doseId,
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<MedicationPlan?>(plan));
        }
    }

    private static CreateMedicationPlanCommand CreateCommand(
        TestFixture fixture,
        Guid? attentionId = null)
    {
        return new CreateMedicationPlanCommand(
            fixture.PetId,
            fixture.ClinicId,
            attentionId,
            "Medication test",
            "Oral",
            "Follow prescription",
            Today,
            Today.AddDays(7),
            "UTC",
            new[]
            {
                new MedicationScheduleInput(
                    new TimeOnly(8, 0),
                    Enum.GetValues<DayOfWeek>(),
                    1m,
                    "tablet")
            });
    }

    [Fact]
    public async Task CreatePlan_AuthorizedVeterinarian_SavesWithOutbox()
    {
        var fixture = new TestFixture(UserRole.Veterinarian);

        var handler = new CreateMedicationPlanHandler(
            fixture.Authorization,
            fixture.ClinicalContext,
            fixture.Time,
            fixture.Commit,
            fixture.Clock);

        var result = await handler.HandleAsync(
            CreateCommand(fixture));

        Assert.NotEqual(Guid.Empty, result.PlanId);
        Assert.Equal(fixture.PetId, result.PetId);
        Assert.Equal(fixture.OwnerId, result.OwnerId);
        Assert.Equal(
            MedicationPlanStatus.Active,
            result.Status);

        await fixture.Repository.Received(1).SaveAsync(
            Arg.Is<MedicationPlan>(x =>
                x.PlanId.Value == result.PlanId),
            Arg.Any<CancellationToken>());

        await fixture.Outbox.Received(1).StageAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Any(x =>
                    x is MedicationPlanPrescribedEvent)),
            Arg.Any<CancellationToken>());

        await fixture.UnitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePlan_UnlinkedVeterinarian_IsRejected()
    {
        var fixture = new TestFixture(UserRole.Veterinarian);

        fixture.Veterinarians.CanPrescribeAsync(
                fixture.VeterinarianId,
                fixture.ClinicId,
                fixture.PetId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var handler = new CreateMedicationPlanHandler(
            fixture.Authorization,
            fixture.ClinicalContext,
            fixture.Time,
            fixture.Commit,
            fixture.Clock);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(CreateCommand(fixture)));

        await fixture.UnitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePlan_InvalidClinicalReference_IsRejected()
    {
        var fixture = new TestFixture(UserRole.Veterinarian);

        var handler = new CreateMedicationPlanHandler(
            fixture.Authorization,
            fixture.ClinicalContext,
            fixture.Time,
            fixture.Commit,
            fixture.Clock);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(
                CreateCommand(fixture, Guid.NewGuid())));

        await fixture.UnitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfigureReminders_OwnerCanEnableThem()
    {
        var fixture = new TestFixture(UserRole.Owner);
        var plan = fixture.CreatePlan();

        fixture.ReturnPlan(plan);

        var handler = new ConfigureMedicationRemindersHandler(
            fixture.Repository,
            fixture.Authorization,
            fixture.Reminders,
            fixture.Commit,
            fixture.Clock);

        var result = await handler.HandleAsync(
            new ConfigureMedicationRemindersCommand(
                plan.PlanId.Value,
                true));

        Assert.True(result.RemindersEnabled);

        await fixture.Outbox.Received(1).StageAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Any(x =>
                    x is MedicationRemindersConfiguredEvent)),
            Arg.Any<CancellationToken>());

        await fixture.UnitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfigureReminders_OtherOwnerCannotModifyPlan()
    {
        var fixture = new TestFixture(
            UserRole.Owner,
            useAnotherOwner: true);

        var plan = fixture.CreatePlan();
        fixture.ReturnPlan(plan);

        var handler = new ConfigureMedicationRemindersHandler(
            fixture.Repository,
            fixture.Authorization,
            fixture.Reminders,
            fixture.Commit,
            fixture.Clock);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(
                new ConfigureMedicationRemindersCommand(
                    plan.PlanId.Value,
                    true)));

        Assert.False(plan.RemindersEnabled);

        await fixture.UnitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAdministration_ValidDose_SavesAndCancelsReminder()
    {
        var fixture = new TestFixture(UserRole.Owner);
        var plan = fixture.CreatePlan();

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            Today,
            new DateTime(
                2026, 10, 23, 8, 0, 0,
                DateTimeKind.Utc),
            FixedNow);

        plan.PullDomainEvents();
        fixture.ReturnDose(dose.DoseId, plan);

        var handler = new RegisterDoseAdministrationHandler(
            fixture.Repository,
            fixture.Authorization,
            fixture.Time,
            fixture.Reminders,
            fixture.Commit,
            fixture.Clock);

        var result = await handler.HandleAsync(
            new RegisterDoseAdministrationCommand(
                dose.DoseId.Value,
                FixedNow.AddMinutes(-5)));

        Assert.NotEqual(
            Guid.Empty,
            result.AdministrationId);

        Assert.Equal(
            MedicationDoseStatus.Administered,
            dose.Status);

        await fixture.Reminders.Received(1)
            .CancelForDoseAsync(
                dose.DoseId.Value,
                Arg.Any<CancellationToken>());

        await fixture.Outbox.Received(1).StageAsync(
            Arg.Is<IReadOnlyCollection<IDomainEvent>>(events =>
                events.Any(x =>
                    x is MedicationDoseAdministeredEvent)),
            Arg.Any<CancellationToken>());

        await fixture.UnitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAdministration_Duplicate_IsRejected()
    {
        var fixture = new TestFixture(UserRole.Owner);
        var plan = fixture.CreatePlan();

        var dose = plan.ScheduleDose(
            plan.Schedules[0].ScheduleId,
            Today,
            new DateTime(
                2026, 10, 23, 8, 0, 0,
                DateTimeKind.Utc),
            FixedNow);

        plan.RegisterAdministration(
            dose.DoseId,
            plan.OwnerId,
            FixedNow.AddMinutes(-10),
            FixedNow.AddMinutes(-5),
            Today);

        plan.PullDomainEvents();
        fixture.ReturnDose(dose.DoseId, plan);

        var handler = new RegisterDoseAdministrationHandler(
            fixture.Repository,
            fixture.Authorization,
            fixture.Time,
            fixture.Reminders,
            fixture.Commit,
            fixture.Clock);

        await Assert.ThrowsAsync<MedicationDomainException>(() =>
            handler.HandleAsync(
                new RegisterDoseAdministrationCommand(
                    dose.DoseId.Value,
                    FixedNow.AddMinutes(-2))));

        await fixture.Reminders.DidNotReceive()
            .CancelForDoseAsync(
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());

        await fixture.UnitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePlan_Cancel_CancelsPendingReminders()
    {
        var fixture = new TestFixture(UserRole.Veterinarian);
        var plan = fixture.CreatePlan();

        fixture.ReturnPlan(plan);

        var handler = new UpdateMedicationPlanHandler(
            fixture.Repository,
            fixture.Authorization,
            fixture.Reminders,
            fixture.Commit,
            fixture.Clock);

        var result = await handler.HandleAsync(
            new UpdateMedicationPlanCommand(
                plan.PlanId.Value,
                MedicationUpdateOperation.Cancel));

        Assert.Equal(
            MedicationPlanStatus.Cancelled,
            result.Status);

        await fixture.Reminders.Received(1)
            .CancelPendingForPlanAsync(
                plan.PlanId.Value,
                Arg.Any<CancellationToken>());

        await fixture.UnitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPlan_OwnerCanReadOwnTreatment()
    {
        var fixture = new TestFixture(UserRole.Owner);
        var plan = fixture.CreatePlan();

        fixture.ReturnPlan(plan);

        var handler = new GetMedicationPlanHandler(
            fixture.Repository,
            fixture.Authorization);

        var result = await handler.HandleAsync(
            new GetMedicationPlanQuery(plan.PlanId.Value));

        Assert.Equal(plan.PlanId.Value, result.PlanId);
        Assert.Equal(plan.PetId.Value, result.PetId);

        await fixture.UnitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDoses_InvalidDateRange_IsRejected()
    {
        var fixture = new TestFixture(UserRole.Owner);

        var handler = new GetMedicationDosesHandler(
            fixture.Repository,
            fixture.Authorization);

        await Assert.ThrowsAsync<MedicationValidationException>(() =>
            handler.HandleAsync(
                new GetMedicationDosesQuery(
                    Guid.NewGuid(),
                    Today.AddDays(2),
                    Today)));
    }

    [Fact]
    public async Task GenerateDoses_SecondRunDoesNotDuplicate()
    {
        var fixture = new TestFixture(UserRole.Veterinarian);
        var plan = fixture.CreatePlan();

        fixture.Repository.FindEligibleForGenerationAsync(
                Arg.Any<DateOnly>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<MedicationPlan>>(
                new[] { plan }));

        var handler = new GenerateMedicationDosesHandler(
            fixture.Repository,
            fixture.Time,
            fixture.Reminders,
            fixture.Commit,
            fixture.Clock);

        var command = new GenerateMedicationDosesCommand(
            Today.AddDays(2),
            20);

        var first = await handler.HandleAsync(command);
        var second = await handler.HandleAsync(command);

        // Hoy ya pasó el horario de las 08:00.
        // Deben generarse las dosis de los dos
        // días siguientes.
        Assert.Equal(2, first);
        Assert.Equal(0, second);

        Assert.Equal(2, plan.Doses.Count);

        await fixture.UnitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TimeService_ConvertsLimaTimeToUtc()
    {
        var time = new MedicationTimeService();

        var actual = time.ConvertToUtc(
            new DateOnly(2026, 10, 23),
            new TimeOnly(8, 0),
            "America/Lima");

        var expected = new DateTime(
            2026, 10, 23, 13, 0, 0,
            DateTimeKind.Utc);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TimeService_UnknownTimeZone_IsRejected()
    {
        var time = new MedicationTimeService();

        Assert.Throws<MedicationValidationException>(() =>
            time.Resolve("Invalid/UnknownZone"));
    }
}
