using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.Events;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Factories;

public static class PetFactory
{
    public static Pet Register(
        OwnerId ownerId,
        string name,
        Species species,
        DateOnly birthDate,
        string? breed = null,
        Sex? sex = null,
        string? tutorName = null,
        string? microchipNumber = null,
        string? insuranceProvider = null,
        string? policyNumber = null,
        string? photoUrl = null)
    {
        // Generate registration timestamp
        var registeredAt = DateTime.UtcNow;

        // Create the Pet aggregate
        var pet = new Pet(
            PetId.New(),
            ownerId,
            name,
            species,
            birthDate,
            breed,
            sex,
            registeredAt,
            tutorName,
            microchipNumber,
            insuranceProvider,
            policyNumber,
            photoUrl
        );

        // Register the domain event
        pet.AddDomainEvent(
            new PetRegisteredEvent(
                pet.PetId,
                pet.OwnerId,
                registeredAt
            )
        );

        return pet;
    }
}