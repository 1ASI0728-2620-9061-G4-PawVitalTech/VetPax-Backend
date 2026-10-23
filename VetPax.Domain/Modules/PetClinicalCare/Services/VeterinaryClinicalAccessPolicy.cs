using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Services;

public sealed class VeterinaryClinicalAccessPolicy
{
    public bool CanAccess(
        AccessRequester requester,
        bool hasActiveClinicMembership,
        bool hasActivePatientLink)
    {
        ArgumentNullException.ThrowIfNull(requester);

        return requester.Role == RequesterRole.Veterinarian
               && requester.UserId != Guid.Empty
               && requester.ClinicId is { } clinicId
               && clinicId.Value != Guid.Empty
               && hasActiveClinicMembership
               && hasActivePatientLink;
    }
}