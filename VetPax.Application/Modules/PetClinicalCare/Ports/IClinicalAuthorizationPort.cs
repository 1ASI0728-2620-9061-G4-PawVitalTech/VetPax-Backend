using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public interface IClinicalAuthorizationPort
{
    Task<VeterinarianId?> GetAuthorizedVeterinarianAsync(
        Guid userId,
        ClinicId clinicId,
        PetId petId,
        CancellationToken cancellationToken = default);

    Task<bool> CanViewEvolutionAsync(
        Guid userId,
        ClinicId clinicId,
        PetId petId,
        CancellationToken cancellationToken = default);
}