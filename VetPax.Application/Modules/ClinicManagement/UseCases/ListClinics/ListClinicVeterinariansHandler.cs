using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.ListClinics;

public sealed class ListClinicVeterinariansHandler(
    IClinicDirectoryPort directory,
    IClinicRepository clinics)
{
    public async Task<IReadOnlyList<ClinicVeterinarianItem>>
        HandleAsync(
            Guid clinicId,
            CancellationToken cancellationToken = default)
    {
        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid clinic ID is required.");
        }

        var exists = await clinics.ExistsByIdAsync(
            new ClinicId(clinicId),
            cancellationToken);

        if (!exists)
        {
            throw new KeyNotFoundException(
                "Clinic not found.");
        }

        return await directory.GetActiveVeterinariansAsync(
            clinicId,
            cancellationToken);
    }
}