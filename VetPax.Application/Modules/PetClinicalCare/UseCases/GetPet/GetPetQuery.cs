using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;

public sealed record GetPetQuery(
    PetId PetId,
    OwnerId RequesterOwnerId
);