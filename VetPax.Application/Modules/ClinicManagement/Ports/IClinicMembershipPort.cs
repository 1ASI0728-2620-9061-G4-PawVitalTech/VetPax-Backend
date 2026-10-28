using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.Ports;

public interface IClinicMembershipPort
{
    Task<bool> IsActiveVeterinarianAsync(
        Guid userId,
        ClinicId clinicId,
        CancellationToken cancellationToken = default);
}