namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public interface IClinicDirectoryPort
{
    Task<bool> ExistsAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);
}