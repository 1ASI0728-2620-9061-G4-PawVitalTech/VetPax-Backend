using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;

namespace VetPax.Application.Modules.MedicationTreatment.Services;

public sealed class MedicationCommitService
{
    private readonly IMedicationPlanRepository _repository;
    private readonly IMedicationOutboxPort _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public MedicationCommitService(
        IMedicationPlanRepository repository,
        IMedicationOutboxPort outbox,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task SaveAsync(
        MedicationPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        await _repository.SaveAsync(
            plan,
            cancellationToken);

        var events = plan.PullDomainEvents().ToArray();

        if (events.Length > 0)
        {
            await _outbox.StageAsync(
                events,
                cancellationToken);
        }

        await _unitOfWork.CommitAsync(
            cancellationToken);
    }
}