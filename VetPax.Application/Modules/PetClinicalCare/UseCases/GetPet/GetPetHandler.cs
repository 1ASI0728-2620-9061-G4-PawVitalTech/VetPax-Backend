using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;

public sealed class GetPetHandler
{
    private readonly IPetRepository _petRepository;
    private readonly IClinicalAuthorizationPort _authorization;

    public GetPetHandler(
        IPetRepository petRepository,
        IClinicalAuthorizationPort authorization)
    {
        _petRepository = petRepository;
        _authorization = authorization;
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
            pet.RegisteredAt,
            pet.TutorName,
            pet.MicrochipNumber,
            pet.InsuranceProvider,
            pet.PolicyNumber,
            pet.PhotoUrl
        );
    }

    public async Task<GetPetResult?> HandleForVeterinarianAsync(
        PetId petId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default)
    {
        if (petId.Value == Guid.Empty || veterinarianId == Guid.Empty)
            throw new ArgumentException("Valid identifiers are required.");

        var authorized = await _authorization.CanViewPetAsync(
            veterinarianId,
            petId,
            cancellationToken);

        if (!authorized)
            throw new UnauthorizedAccessException(
                "You do not have access to this pet.");

        var pet = await _petRepository.FindByIdAsync(
            petId,
            cancellationToken);

        return pet is null
            ? null
            : new GetPetResult(
                pet.PetId,
                pet.Name,
                pet.Species,
                pet.BirthDate,
                pet.Breed,
                pet.Sex,
                pet.RegisteredAt,
                pet.TutorName,
                pet.MicrochipNumber,
                pet.InsuranceProvider,
                pet.PolicyNumber,
                pet.PhotoUrl);
    }
}