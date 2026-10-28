
using NSubstitute;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Application.Modules.ClinicManagement.UseCases.AssignVeterinarian;
using VetPax.Application.Modules.ClinicManagement.UseCases.ProvisionClinic;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using Xunit;

namespace VetPax.Tests.Application.ClinicManagement;

public sealed class ClinicApplicationTests
{
    [Fact]
    public async Task ProvisionClinic_UsesVerifiedAdministrator()
    {
        var adminId = Guid.NewGuid();

        var currentUser =
            Substitute.For<IAuthenticatedUserPort>();

        currentUser.GetCurrentAsync(
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthenticatedIdentity?>(
                new AuthenticatedIdentity(
                    new UserId(adminId),
                    "admin@test.local",
                    UserRole.ClinicAdministrator)));

        var repository = Substitute.For<IClinicRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new ProvisionClinicHandler(
            currentUser,
            repository,
            unitOfWork);

        var result = await handler.HandleAsync(
            new ProvisionClinicCommand(
                "VetPax",
                "Av. Central 123",
                null,
                null,
                Array.Empty<DailyHoursData>()));

        Assert.NotEqual(Guid.Empty, result.ClinicId);

        await repository.Received(1).SaveAsync(
            Arg.Is<Clinic>(c =>
                c.IsManagedBy(adminId)),
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignVeterinarian_RejectsUnverifiedAccount()
    {
        var adminId = Guid.NewGuid();
        var clinicId = ClinicId.New();
        var veterinarianId = Guid.NewGuid();

        var clinic = new Clinic(
            clinicId,
            new AdministratorId(adminId),
            new ClinicGeneralData(
                "VetPax",
                "Av. Central 123"),
            new OpeningSchedule(
                Array.Empty<DailyOpeningHours>()));

        var currentUser =
            Substitute.For<IAuthenticatedUserPort>();

        currentUser.GetCurrentAsync(
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<AuthenticatedIdentity?>(
                new AuthenticatedIdentity(
                    new UserId(adminId),
                    "admin@test.local",
                    UserRole.ClinicAdministrator)));

        var repository = Substitute.For<IClinicRepository>();

        repository.FindByIdAsync(
            clinicId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Clinic?>(clinic));

        var directory =
            Substitute.For<IVeterinarianIdentityDirectoryPort>();

        directory.IsActiveVeterinarianAsync(
            veterinarianId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var writer =
            Substitute.For<IClinicVeterinarianWriterPort>();

        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new AssignVeterinarianHandler(
            currentUser,
            repository,
            directory,
            writer,
            unitOfWork);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(
                new AssignVeterinarianCommand(
                    clinicId.Value,
                    veterinarianId)));

        await writer.DidNotReceive().LinkAsync(
            Arg.Any<ClinicId>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
