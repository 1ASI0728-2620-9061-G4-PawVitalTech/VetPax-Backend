using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

public sealed class DenyAllClinicalAuthorizationAdapter
    : IClinicalAuthorizationPort
{
    public Task<VeterinarianId?> GetAuthorizedVeterinarianAsync(
        Guid userId,
        ClinicId clinicId,
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<VeterinarianId?>(null);
    }

    public Task<bool> CanViewEvolutionAsync(
        Guid userId,
        ClinicId clinicId,
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(false);
    }
}