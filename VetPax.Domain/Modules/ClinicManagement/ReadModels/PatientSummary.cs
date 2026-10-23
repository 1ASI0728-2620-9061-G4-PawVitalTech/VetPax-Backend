namespace VetPax.Domain.Modules.ClinicManagement.ReadModels;

public sealed record PatientSummary(
    Guid PetId,
    string Name,
    string Species,
    DateTime? LastAttentionDate
);