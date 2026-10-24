using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.Ports;

public interface IClinicalAppointmentLinkPort
{
    Task StageVerifiedLinkAsync(
        Guid appointmentId,
        Guid clinicalAttentionId,
        PetId petId,
        ClinicId clinicId,
        VeterinarianId veterinarianId,
        CancellationToken cancellationToken = default);
}

public sealed class ClinicalAppointmentLinkConflictException
    : Exception
{
    public ClinicalAppointmentLinkConflictException(string message)
        : base(message)
    {
    }
}