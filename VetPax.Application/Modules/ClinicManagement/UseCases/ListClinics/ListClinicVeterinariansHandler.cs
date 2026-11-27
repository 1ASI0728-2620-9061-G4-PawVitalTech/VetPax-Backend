using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.ListClinics;

public sealed class ListClinicVeterinariansHandler(
    IClinicDirectoryPort directory,
    IClinicRepository clinics,
    IVeterinarianIdentityDirectoryPort identities)
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

        var members = await directory.GetActiveVeterinariansAsync(
            clinicId, cancellationToken);

        var result = new List<ClinicVeterinarianItem>(members.Count);
        foreach (var member in members)
        {
            var profile = await identities.GetVeterinarianProfileAsync(
                member.VeterinarianId, cancellationToken);
            result.Add(member with
            {
                FullName = profile?.FullName,
                Specialty = profile?.Specialty,
                LicenseNumber = profile?.LicenseNumber
            });
        }
        return result;
    }
}