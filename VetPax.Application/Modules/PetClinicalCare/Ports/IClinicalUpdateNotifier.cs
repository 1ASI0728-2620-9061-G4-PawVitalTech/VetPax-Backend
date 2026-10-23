using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public interface IClinicalUpdateNotifier
{
    Task NotifyClinicalRecordUpdatedAsync(
        PetId petId,
        ClinicalRecordId clinicalRecordId,
        CancellationToken cancellationToken = default);
}