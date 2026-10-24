using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;
using VetPax.Domain.Modules.NutritionManagement.Repositories;

namespace VetPax.Application.Modules.NutritionManagement.Services;

public sealed class NutritionCommitService
{
    private readonly IFeedingPlanRepository _repository;
    private readonly INutritionOutboxPort _outbox;
    private readonly INutritionUnitOfWork _unitOfWork;

    public NutritionCommitService(
        IFeedingPlanRepository repository,
        INutritionOutboxPort outbox,
        INutritionUnitOfWork unitOfWork)
    {
        _repository = repository;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task CommitAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        // Repository.SaveAsync stages tracked changes.
        // It must not call SaveChanges independently.
        await _repository.SaveAsync(plan, cancellationToken);

        var events = plan.PullDomainEvents();

        if (events.Count > 0)
            await _outbox.StageAsync(events, cancellationToken);

        // Save the aggregate and outbox atomically
        // through the same DbContext.
        await _unitOfWork.CommitAsync(cancellationToken);
    }
}