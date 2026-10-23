using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record RegisterPetRequest(
    string Name,
    Species Species,
    DateOnly BirthDate,
    string? Breed = null,
    Sex? Sex = null
);