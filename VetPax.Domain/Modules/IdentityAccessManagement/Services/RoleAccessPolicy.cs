using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Domain.Modules.IdentityAccessManagement.Services;

public sealed class RoleAccessPolicy
{
    public bool CanAccess(
        UserRole role,
        AccessPermission permission)
    {
        if (!Enum.IsDefined(role) ||
            !Enum.IsDefined(permission))
            return false;

        return role switch
        {
            UserRole.Owner => permission is
                AccessPermission.ManageOwnPets or
                AccessPermission.ViewOwnClinicalHistory or
                AccessPermission.ScheduleOwnAppointments,

            UserRole.Veterinarian => permission is
                AccessPermission.ViewAuthorizedPatients or
                AccessPermission.RegisterClinicalAttention or
                AccessPermission.ViewClinicalEvolution,

            UserRole.ClinicAdministrator => permission is
                AccessPermission.ManageClinic,

            _ => false
        };
    }
}