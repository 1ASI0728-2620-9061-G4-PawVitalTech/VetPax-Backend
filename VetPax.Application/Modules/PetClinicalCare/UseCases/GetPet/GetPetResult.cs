using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;

public sealed record GetPetResult(
    PetId PetId,
    string Name,
    Species Species,
    DateOnly BirthDate,
    string? Breed,
    Sex? Sex,
    DateTime RegisteredAt
);