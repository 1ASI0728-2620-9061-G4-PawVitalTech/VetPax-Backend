using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.Ports;

public interface IClinicVeterinarianWriterPort
{
    Task LinkAsync(
        ClinicId clinicId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default);
}