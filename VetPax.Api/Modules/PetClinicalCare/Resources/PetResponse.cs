namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record PetResponse(
    Guid PetId,
    string Name,
    string Species,
    DateOnly BirthDate,
    string? Breed,
    string? Sex,
    DateTime RegisteredAt
);