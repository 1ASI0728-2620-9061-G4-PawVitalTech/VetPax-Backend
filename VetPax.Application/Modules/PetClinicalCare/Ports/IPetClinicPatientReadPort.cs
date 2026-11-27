namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public sealed record LinkedClinicPatient(
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
    string? PhotoUrl);

public interface IPetClinicPatientReadPort
{
    Task<IReadOnlyList<LinkedClinicPatient>> FindByClinicAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);
}