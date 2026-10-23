using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Repositories;

public interface IPetRepository
{
    Task SaveAsync(
        Pet pet,
        CancellationToken cancellationToken = default
    );

    Task<Pet?> FindByIdAsync(
        PetId petId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Pet>> FindByOwnerIdAsync(
        OwnerId ownerId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsByIdAsync(
        PetId petId,
        CancellationToken cancellationToken = default
    );
}