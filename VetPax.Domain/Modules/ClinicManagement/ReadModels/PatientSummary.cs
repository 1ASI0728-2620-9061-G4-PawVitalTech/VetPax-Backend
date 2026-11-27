namespace VetPax.Domain.Modules.ClinicManagement.ReadModels;

public sealed record PatientSummary(
    Guid PetId,
    string Name,
    string Species,
    DateTime? LastAttentionDate,
    DateOnly BirthDate,
    string? Breed,
    string? Sex,
    Guid OwnerId,
    string? TutorName,
    string? MicrochipNumber,
    string? InsuranceProvider,
    string? PolicyNumber,
    string? PhotoUrl
);