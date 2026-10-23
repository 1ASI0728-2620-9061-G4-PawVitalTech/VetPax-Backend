
using NSubstitute;
using Xunit;
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;
using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.Factories;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Tests.Application.PetClinicalCare;

public sealed class PetClinicalCareHandlerTests
{
    [Fact]
    public async Task RegisterPet_ShouldSavePetAndClinicalRecord()
    {
        var pets = Substitute.For<IPetRepository>();
        var records = Substitute.For<IClinicalRecordRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new RegisterPetHandler(
            pets, records, unitOfWork);

        var command = new RegisterPetCommand(
            new OwnerId(Guid.NewGuid()),
            "Luna",
            Species.Dog,
            new DateOnly(2022, 5, 10));

        var result = await handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result.PetId.Value);
        Assert.NotEqual(
            Guid.Empty,
            result.ClinicalRecordId.Value);

        await pets.Received(1).SaveAsync(
            Arg.Is<Pet>(p => p.PetId == result.PetId),
            Arg.Any<CancellationToken>());

        await records.Received(1).SaveAsync(
            Arg.Is<ClinicalRecord>(
                r => r.PetId == result.PetId),
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1).CommitAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAttention_ShouldRejectUnauthorizedVet()
    {
        var pets = Substitute.For<IPetRepository>();
        var records = Substitute.For<IClinicalRecordRepository>();
        var authorization =
            Substitute.For<IClinicalAuthorizationPort>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var events = Substitute.For<IDomainEventPublisher>();
        var notifier =
            Substitute.For<IClinicalUpdateNotifier>();

        var pet = PetFactory.Register(
            new OwnerId(Guid.NewGuid()),
            "Luna",
            Species.Dog,
            new DateOnly(2022, 5, 10));

        pets.FindByIdAsync(
            pet.PetId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Pet?>(pet));

        var userId = Guid.NewGuid();
        var clinicId = new ClinicId(Guid.NewGuid());

        authorization.GetAuthorizedVeterinarianAsync(
            userId,
            clinicId,
            pet.PetId,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<VeterinarianId?>(null));

        var handler = new RegisterClinicalAttentionHandler(
            pets,
            records,
            authorization,
            unitOfWork,
            events,
            notifier);

        var command = new RegisterClinicalAttentionCommand(
            pet.PetId,
            new AccessRequester(
                userId,
                RequesterRole.Veterinarian,
                clinicId),
            DateTime.UtcNow,
            "Checkup",
            "Healthy",
            "Observation",
            null,
            null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(command));

        await unitOfWork.DidNotReceive().CommitAsync(
            Arg.Any<CancellationToken>());
    }
}
