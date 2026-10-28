
using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Adapters;

public sealed class NutritionPatientAdapter : INutritionPatientPort
{
    private readonly IPetRepository _pets;

    public NutritionPatientAdapter(IPetRepository pets)
    {
        _pets = pets;
    }

    public async Task<Guid?> GetOwnerIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (petId == Guid.Empty)
            return null;

        var pet = await _pets.FindByIdAsync(
            new PetId(petId),
            cancellationToken);

        return pet?.OwnerId.Value;
    }
}

public sealed class NutritionVeterinarianAdapter
    : INutritionVeterinarianPort
{
    private readonly VetPaxDbContext _db;

    public NutritionVeterinarianAdapter(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanPrescribeAsync(
        Guid veterinarianId,
        Guid clinicId,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (veterinarianId == Guid.Empty ||
            clinicId == Guid.Empty ||
            petId == Guid.Empty)
        {
            return false;
        }

        var veterinarianIsActive =
            await _db.ClinicVeterinarians
                .AsNoTracking()
                .AnyAsync(
                    x => x.VeterinarianId == veterinarianId &&
                         x.ClinicId == clinicId &&
                         x.IsActive,
                    cancellationToken);

        if (!veterinarianIsActive)
            return false;

        return await _db.PetClinicLinks
            .AsNoTracking()
            .AnyAsync(
                x => x.PetId == petId &&
                     x.ClinicId == clinicId &&
                     x.IsActive,
                cancellationToken);
    }
}
