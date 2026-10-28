
using NSubstitute;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.AppointmentManagement.Exceptions;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.AppointmentManagement.Services;
using VetPax.Application.Modules.AppointmentManagement.UseCases.ScheduleAppointment;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Application.AppointmentManagement;

public sealed class ScheduleAppointmentHandlerTests
{
    private static (
        ScheduleAppointmentHandler Handler,
        IAppointmentRepository Repository,
        IAppointmentNotificationPort Notifications,
        IUnitOfWork UnitOfWork)
        CreateHandler(bool occupied)
    {
        var userId = Guid.NewGuid();

        var currentUser =
            Substitute.For<IAuthenticatedUserPort>();

        currentUser.GetCurrentAsync(
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthenticatedIdentity?>(
                new AuthenticatedIdentity(
                    new UserId(userId),
                    "owner@test.local",
                    UserRole.Owner)));

        var patients = Substitute.For<IAppointmentPatientPort>();

        patients.IsOwnedAndLinkedAsync(
            Arg.Any<Guid>(),
            userId,
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var clinics = Substitute.For<IAppointmentClinicPort>();

        clinics.IsActiveVeterinarianAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        clinics.IsWithinOpeningHoursAsync(
            Arg.Any<Guid>(),
            Arg.Any<AppointmentSlot>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var repository =
            Substitute.For<IAppointmentRepository>();

        repository.HasOverlapAsync(
            Arg.Any<AppointmentVeterinarianId>(),
            Arg.Any<AppointmentSlot>(),
            Arg.Any<AppointmentId?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(occupied));

        var notifications =
            Substitute.For<IAppointmentNotificationPort>();

        var unitOfWork = Substitute.For<IUnitOfWork>();

        var access = new AppointmentAccessService(
            currentUser,
            patients,
            clinics,
            TimeProvider.System);

        var handler = new ScheduleAppointmentHandler(
            repository,
            access,
            notifications,
            unitOfWork);

        return (
            handler,
            repository,
            notifications,
            unitOfWork);
    }

    private static ScheduleAppointmentCommand ValidCommand()
    {
        return new ScheduleAppointmentCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            new TimeOnly(9, 0),
            "Control general");
    }

    [Fact]
    public async Task Schedule_ValidSlot_SavesAppointment()
    {
        var sut = CreateHandler(occupied: false);

        var result = await sut.Handler.HandleAsync(
            ValidCommand());

        Assert.NotEqual(Guid.Empty, result.AppointmentId);

        await sut.Repository.Received(1).SaveAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());

        await sut.Notifications.Received(1)
            .StageChangesAsync(
                Arg.Any<Appointment>(),
                AppointmentNotificationAction.Schedule,
                Arg.Any<IReadOnlyCollection<
                    VetPax.Domain.Shared.Events.IDomainEvent>>(),
                Arg.Any<CancellationToken>());

        await sut.UnitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Schedule_OccupiedSlot_RejectsReservation()
    {
        var sut = CreateHandler(occupied: true);

        await Assert.ThrowsAsync<AppointmentConflictException>(
            () => sut.Handler.HandleAsync(
                ValidCommand()));

        await sut.Repository.DidNotReceive().SaveAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());

        await sut.UnitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }
}
