using VetPax.Application.Modules.AppointmentManagement.Models;

namespace VetPax.Application.Modules.AppointmentManagement.Ports;

public interface IAppointmentPatientPort
{
    Task<bool> IsOwnedAndLinkedAsync(
        Guid petId,
        Guid ownerId,
        Guid clinicId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, AppointmentPatientSummary>>
        GetSummariesAsync(
            IReadOnlyCollection<Guid> petIds,
            CancellationToken cancellationToken = default);
}