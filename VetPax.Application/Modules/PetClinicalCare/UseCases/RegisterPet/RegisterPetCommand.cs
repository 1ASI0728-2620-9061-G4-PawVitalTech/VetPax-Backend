using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;

public sealed record RegisterPetCommand(
    OwnerId OwnerId,
    string Name,
    Species Species,
    DateOnly BirthDate,
    string? Breed = null,
    Sex? Sex = null
);