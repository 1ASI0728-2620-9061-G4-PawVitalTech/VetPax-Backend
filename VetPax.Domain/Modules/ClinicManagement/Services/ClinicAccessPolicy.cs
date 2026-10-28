using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Domain.Modules.ClinicManagement.Services;

public sealed class ClinicAccessPolicy
{
    public bool CanManageProfile(
        ClinicAccessRequester requester,
        Clinic clinic)
    {
        return requester.IsAdministrator() &&
               requester.ClinicId == clinic.ClinicId &&
               clinic.IsManagedBy(requester.UserId);
    }

    public bool CanViewProfile(
        ClinicAccessRequester requester,
        ClinicId clinicId)
    {
        return requester.ClinicId == clinicId &&
               (requester.IsAdministrator() ||
                requester.IsVeterinarian());
    }

    public bool CanViewPatients(
        ClinicAccessRequester requester,
        ClinicId clinicId)
    {
        return requester.IsVeterinarian() &&
               requester.ClinicId == clinicId;
    }
}