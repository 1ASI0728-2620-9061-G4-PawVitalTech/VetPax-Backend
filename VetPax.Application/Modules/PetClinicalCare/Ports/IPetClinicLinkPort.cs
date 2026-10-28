using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public interface IPetClinicLinkPort
{
    Task LinkAsync(
        PetId petId,
        Guid clinicId,
        CancellationToken cancellationToken = default);
}