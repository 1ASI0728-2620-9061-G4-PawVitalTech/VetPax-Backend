
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
    private readonly IClinicalAppointmentLinkPort? _appointmentLinks;

    public RegisterClinicalAttentionHandler(
        IPetRepository pets,
        IClinicalRecordRepository records,
        IClinicalAuthorizationPort authorization,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher events,
        IClinicalUpdateNotifier notifier,
        IClinicalAppointmentLinkPort? appointmentLinks = null)
    {
        _pets = pets;
        _records = records;
        _authorization = authorization;
        _unitOfWork = unitOfWork;
        _events = events;
        _notifier = notifier;
        _appointmentLinks = appointmentLinks;
    }

    // Se conserva la firma original de US03.
    public Task<RegisterClinicalAttentionResult> HandleAsync(
        RegisterClinicalAttentionCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleCoreAsync(
            command,
            null,
            cancellationToken);
    }

    // Nuevo flujo: atención asociada a una cita.
    public Task<RegisterClinicalAttentionResult> HandleForAppointmentAsync(
        RegisterClinicalAttentionCommand command,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (appointmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid appointment ID is required.");
        }

        if (_appointmentLinks is null)
        {
            throw new InvalidOperationException(
                "Clinical appointment integration is not configured.");
        }

        return HandleCoreAsync(
            command,
            appointmentId,
            cancellationToken);
    }

    private async Task<RegisterClinicalAttentionResult> HandleCoreAsync(
        RegisterClinicalAttentionCommand command,
        Guid? appointmentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var requester = command.Requester;

        if (requester is null ||
            !requester.IsVeterinarian() ||
            requester.UserId == Guid.Empty ||
            requester.ClinicId is not { } clinicId ||
            clinicId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian authentication is required.");
        }

        var pet = await _pets.FindByIdAsync(
            command.PetId,
            cancellationToken);

        if (pet is null)
        {
            throw new KeyNotFoundException(
                "Pet not found.");
        }

        var veterinarianId =
            await _authorization.GetAuthorizedVeterinarianAsync(
                requester.UserId,
                clinicId,
                command.PetId,
                cancellationToken);

        if (veterinarianId is null ||
            veterinarianId.Value.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian is not authorized for this patient.");
        }

        var record = await _records.FindByPetIdAsync(
            command.PetId,
            cancellationToken);

        if (record is null)
        {
            throw new KeyNotFoundException(
                "Clinical record not found.");
        }

        var indicators = command.Indicators?
            .Select(i => new ClinicalIndicator(
                i.Name,
                i.Value,
                i.Unit))
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

        // Verificación y asociación antes del commit.
        // Si falla, no se registra la nueva atención.
        if (appointmentId.HasValue)
        {
            await _appointmentLinks!.StageVerifiedLinkAsync(
                appointmentId.Value,
                attention.AttentionId.Value,
                command.PetId,
                clinicId,
                veterinarianId.Value,
                cancellationToken);
        }

        record.RegisterAttention(attention);

        await _records.SaveAsync(
            record,
            cancellationToken);

        // Confirma la atención y el vínculo preparado
        // utilizando el mismo DbContext / UnitOfWork.
        await _unitOfWork.CommitAsync(
            cancellationToken);

        await _events.PublishAsync(
            record.PullDomainEvents(),
            cancellationToken);

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
