using VetPax.Domain.Modules.PetClinicalCare.Repositories;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;

public sealed class GetPetHandler
{
    private readonly IPetRepository _petRepository;

    public GetPetHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<GetPetResult?> HandleAsync(
        GetPetQuery query,
        CancellationToken cancellationToken = default)
    {
        var pet = await _petRepository.FindByIdAsync(
            query.PetId,
            cancellationToken
        );

        if (pet is null)
            return null;

        if (!pet.IsOwnedBy(query.RequesterOwnerId))
            throw new UnauthorizedAccessException(
                "You do not have access to this pet."
            );

        return new GetPetResult(
            pet.PetId,
            pet.Name,
            pet.Species,
            pet.BirthDate,
            pet.Breed,
            pet.Sex,
            pet.RegisteredAt
        );
    }
}