using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Services;

public sealed class ClinicalAccessPolicy
{
    public bool CanAccess(
        AccessRequester requester,
        Pet pet)
    {
        ArgumentNullException.ThrowIfNull(requester);
        ArgumentNullException.ThrowIfNull(pet);

        if (requester.IsOwner())
        {
            return pet.IsOwnedBy(
                new OwnerId(requester.UserId)
            );
        }

        // Veterinary access requires a verified
        // clinic-patient relationship.
        // Deny until that authorization is integrated.
        return false;
    }
}