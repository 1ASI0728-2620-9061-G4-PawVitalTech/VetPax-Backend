using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetMyPets;

public sealed class GetMyPetsHandler(IPetRepository pets)
{
    public async Task<IReadOnlyList<GetPetResult>> HandleAsync(
        OwnerId ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid owner ID is required.");

        var ownedPets = await pets.FindByOwnerIdAsync(
            ownerId,
            cancellationToken);

        return ownedPets
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.PetId.Value)
            .Select(p => new GetPetResult(
                p.PetId,
                p.Name,
                p.Species,
                p.BirthDate,
                p.Breed,
                p.Sex,
                p.RegisteredAt))
            .ToArray();
    }
}