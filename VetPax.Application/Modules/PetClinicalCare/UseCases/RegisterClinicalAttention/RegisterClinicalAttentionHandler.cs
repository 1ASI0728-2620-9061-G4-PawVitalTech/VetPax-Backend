
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Entities;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterClinicalAttention;

public sealed class RegisterClinicalAttentionHandler
{
    private readonly IPetRepository _pets;
    private readonly IClinicalRecordRepository _records;
    private readonly IClinicalAuthorizationPort _authorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _events;
    private readonly IClinicalUpdateNotifier _notifier;

    public RegisterClinicalAttentionHandler(
        IPetRepository pets,
        IClinicalRecordRepository records,
        IClinicalAuthorizationPort authorization,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher events,
        IClinicalUpdateNotifier notifier)
    {
        _pets = pets;
        _records = records;
        _authorization = authorization;
        _unitOfWork = unitOfWork;
        _events = events;
        _notifier = notifier;
    }

    public async Task<RegisterClinicalAttentionResult> HandleAsync(
        RegisterClinicalAttentionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var requester = command.Requester;
        if (requester is null ||
            !requester.IsVeterinarian() ||
            requester.ClinicId is not { } clinicId ||
            clinicId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException();
        }

        var pet = await _pets.FindByIdAsync(
            command.PetId, cancellationToken);

        if (pet is null)
            throw new KeyNotFoundException("Pet not found.");

        var veterinarianId =
            await _authorization.GetAuthorizedVeterinarianAsync(
                requester.UserId,
                clinicId,
                command.PetId,
                cancellationToken);

        if (veterinarianId is null ||
            veterinarianId.Value.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException();
        }

        var record = await _records.FindByPetIdAsync(
            command.PetId, cancellationToken);

        if (record is null)
            throw new KeyNotFoundException(
                "Clinical record not found.");

        var indicators = command.Indicators?
            .Select(i => new ClinicalIndicator(
                i.Name, i.Value, i.Unit))
            .ToArray();

        var attention = new ClinicalAttention(
            AttentionId.New(),
            clinicId,
            veterinarianId.Value,
            command.AttentionDate,
            command.Reason,
            command.Diagnosis,
            command.TreatmentSummary,
            command.Observations,
            indicators
        );

        record.RegisterAttention(attention);

        await _records.SaveAsync(record, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _events.PublishAsync(
            record.PullDomainEvents(), cancellationToken);

        await _notifier.NotifyClinicalRecordUpdatedAsync(
            record.PetId,
            record.ClinicalRecordId,
            cancellationToken);

        return new RegisterClinicalAttentionResult(
            attention.AttentionId.Value,
            record.ClinicalRecordId.Value,
            record.PetId.Value
        );
    }
}
