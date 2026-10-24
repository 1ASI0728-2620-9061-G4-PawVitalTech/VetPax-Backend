
using NSubstitute;
using VetPax.Application.Modules.AdherenceGamification.Commands;
using VetPax.Application.Modules.AdherenceGamification.Handlers;
using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Application.Modules.AdherenceGamification.Queries;
using VetPax.Application.Modules.AdherenceGamification.Services;
using VetPax.Domain.Modules.AdherenceGamification.Aggregates;
using VetPax.Domain.Modules.AdherenceGamification.Enums;
using VetPax.Domain.Modules.AdherenceGamification.Repositories;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;
using Xunit;

namespace VetPax.Tests.Application.AdherenceGamification;

public sealed class AdherenceApplicationTests
{
    private static readonly DateTime Now =
        new(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);

    private static readonly AdherencePolicy Policy =
        new(2, 2m, 4m, 8m, false);

    [Fact]
    public async Task DuplicateEvent_DoesNotCommit()
    {
        var evidence = Substitute.For<IAdherenceEvidenceStore>();
        var repository = Substitute.For<IAdherenceRepository>();
        var calculator = Substitute.For<IAdherenceScoreCalculator>();
        var outbox = Substitute.For<IAdherenceOutbox>();
        var unitOfWork = Substitute.For<IAdherenceUnitOfWork>();

        var command = ValidCommand();

        evidence.AlreadyProcessedAsync(
            command, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new IngestAdherenceEvidenceHandler(
            evidence, repository, calculator,
            outbox, unitOfWork, Policy, TimeProvider.System);

        var result = await handler.HandleAsync(command);

        Assert.False(result);

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NewEvidence_UpdatesOwnerProgress()
    {
        var evidence = Substitute.For<IAdherenceEvidenceStore>();
        var repository = Substitute.For<IAdherenceRepository>();
        var calculator = Substitute.For<IAdherenceScoreCalculator>();
        var outbox = Substitute.For<IAdherenceOutbox>();
        var unitOfWork = Substitute.For<IAdherenceUnitOfWork>();

        var command = ValidCommand();

        evidence.CountIncludingStagedAsync(
            command.OwnerId, Arg.Any<CancellationToken>())
            .Returns(2);

        calculator.CalculateAsync(
            command.OwnerId, 2, Arg.Any<CancellationToken>())
            .Returns(new AdherenceScore(2));

        repository.FindByOwnerAsync(
            command.OwnerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Adherence?>(null));

        var handler = new IngestAdherenceEvidenceHandler(
            evidence, repository, calculator,
            outbox, unitOfWork, Policy, TimeProvider.System);

        var result = await handler.HandleAsync(command);

        Assert.True(result);

        await repository.Received(1).StageSaveAsync(
            Arg.Is<Adherence>(x =>
                x.EvidenceCount == 2 &&
                x.Level == ConstancyLevel.Bronze),
            Arg.Any<CancellationToken>());

        await unitOfWork.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OwnerWithoutEvidence_HasNoLevel()
    {
        var currentOwner = Substitute.For<IAdherenceCurrentOwner>();
        var repository = Substitute.For<IAdherenceRepository>();

        var ownerId = Guid.NewGuid();

        currentOwner.RequireOwnerIdAsync(
            Arg.Any<CancellationToken>())
            .Returns(ownerId);

        repository.FindByOwnerAsync(
            ownerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Adherence?>(null));

        var handler = new GetMyAdherenceHandler(
            currentOwner, repository, Policy);

        var result = await handler.HandleAsync(
            new GetMyAdherenceQuery());

        Assert.Equal(ownerId, result.OwnerId);
        Assert.False(result.HasSufficientEvidence);
        Assert.Null(result.Score);
        Assert.Null(result.Level);
    }

    [Fact]
    public async Task Consumer_MapsEvidenceKindsCorrectly()
    {
        var evidence = Substitute.For<IAdherenceEvidenceStore>();
        var repository = Substitute.For<IAdherenceRepository>();
        var calculator = Substitute.For<IAdherenceScoreCalculator>();
        var outbox = Substitute.For<IAdherenceOutbox>();
        var unitOfWork = Substitute.For<IAdherenceUnitOfWork>();

        evidence.AlreadyProcessedAsync(
            Arg.Any<IngestAdherenceEvidenceCommand>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new IngestAdherenceEvidenceHandler(
            evidence, repository, calculator,
            outbox, unitOfWork, Policy, TimeProvider.System);

        var consumer = new AdherenceEvidenceConsumer(handler);
        var ownerId = Guid.NewGuid();

        await consumer.OnAppointmentAttendedAsync(
            Guid.NewGuid(), ownerId, Guid.NewGuid(), Now);

        await consumer.OnDoseAdministeredAsync(
            Guid.NewGuid(), ownerId, Guid.NewGuid(), Now);

        await evidence.Received(1).AlreadyProcessedAsync(
            Arg.Is<IngestAdherenceEvidenceCommand>(x =>
                x.Kind ==
                AdherenceEvidenceKind.VeterinaryAppointmentAttended),
            Arg.Any<CancellationToken>());

        await evidence.Received(1).AlreadyProcessedAsync(
            Arg.Is<IngestAdherenceEvidenceCommand>(x =>
                x.Kind ==
                AdherenceEvidenceKind.MedicationDoseAdministered),
            Arg.Any<CancellationToken>());
    }

    private static IngestAdherenceEvidenceCommand ValidCommand()
    {
        return new IngestAdherenceEvidenceCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AdherenceEvidenceKind.MedicationDoseAdministered,
            Guid.NewGuid(),
            Now);
    }
}
