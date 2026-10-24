
using VetPax.Application.Modules.AdherenceGamification.Commands;
using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Domain.Modules.AdherenceGamification.Aggregates;
using VetPax.Domain.Modules.AdherenceGamification.Repositories;
using VetPax.Domain.Modules.AdherenceGamification.ValueObjects;

namespace VetPax.Application.Modules.AdherenceGamification.Handlers;

public sealed class IngestAdherenceEvidenceHandler
{
    private readonly IAdherenceEvidenceStore _evidence;
    private readonly IAdherenceRepository _repository;
    private readonly IAdherenceScoreCalculator _calculator;
    private readonly IAdherenceOutbox _outbox;
    private readonly IAdherenceUnitOfWork _unitOfWork;
    private readonly AdherencePolicy _policy;
    private readonly TimeProvider _clock;

    public IngestAdherenceEvidenceHandler(
        IAdherenceEvidenceStore evidence,
        IAdherenceRepository repository,
        IAdherenceScoreCalculator calculator,
        IAdherenceOutbox outbox,
        IAdherenceUnitOfWork unitOfWork,
        AdherencePolicy policy,
        TimeProvider clock)
    {
        _evidence = evidence;
        _repository = repository;
        _calculator = calculator;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _policy = policy;
        _clock = clock;
    }

    public async Task<bool> HandleAsync(
        IngestAdherenceEvidenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.EventId == Guid.Empty ||
            command.OwnerId == Guid.Empty ||
            command.SourceId == Guid.Empty ||
            !Enum.IsDefined(command.Kind) ||
            command.OccurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Invalid adherence evidence.");
        }

        if (await _evidence.AlreadyProcessedAsync(
                command, cancellationToken))
        {
            return false;
        }

        await _evidence.StageAsync(
            command, cancellationToken);

        var evidenceCount =
            await _evidence.CountIncludingStagedAsync(
                command.OwnerId, cancellationToken);

        var score = await _calculator.CalculateAsync(
            command.OwnerId,
            evidenceCount,
            cancellationToken);

        var utcNow = _clock.GetUtcNow().UtcDateTime;

        var adherence = await _repository.FindByOwnerAsync(
                command.OwnerId, cancellationToken)
            ?? Adherence.Create(command.OwnerId, utcNow);

        adherence.Recalculate(
            evidenceCount,
            score,
            _policy,
            utcNow);

        await _repository.StageSaveAsync(
            adherence, cancellationToken);

        await _outbox.StageAsync(
            adherence.PullDomainEvents(),
            cancellationToken);

        await _unitOfWork.CommitAsync(
            cancellationToken);

        return true;
    }
}
