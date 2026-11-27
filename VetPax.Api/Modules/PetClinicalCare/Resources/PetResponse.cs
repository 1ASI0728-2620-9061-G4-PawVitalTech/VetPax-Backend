namespace VetPax.Api.Modules.PetClinicalCare.Resources;

public sealed record PetResponse(
    Guid PetId,
    string Name,
    string Species,
    DateOnly BirthDate,
    string? Breed,
    string? Sex,
    DateTime RegisteredAt,
    string? TutorName = null,
    string? MicrochipNumber = null,
    string? InsuranceProvider = null,
    string? PolicyNumber = null,
    string? PhotoUrl = null
);