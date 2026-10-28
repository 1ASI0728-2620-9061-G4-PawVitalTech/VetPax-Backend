namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public sealed record LinkedClinicPatient(
    Guid PetId,
    string Name,
    string Species,
    DateTime? LastAttentionDate);

public interface IPetClinicPatientReadPort
{
    Task<IReadOnlyList<LinkedClinicPatient>> FindByClinicAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);
}