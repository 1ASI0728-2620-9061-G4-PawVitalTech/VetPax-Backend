namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public interface IClinicalRelationshipPort
{
    Task<bool> HasActiveVeterinarianMembershipAsync(
        Guid veterinarianId,
        Guid clinicId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActivePatientLinkAsync(
        Guid petId,
        Guid clinicId,
        CancellationToken cancellationToken = default);
}