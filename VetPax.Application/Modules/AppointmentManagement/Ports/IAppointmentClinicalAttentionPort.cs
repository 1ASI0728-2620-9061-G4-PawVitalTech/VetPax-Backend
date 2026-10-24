namespace VetPax.Application.Modules.AppointmentManagement.Ports;

public interface IAppointmentClinicalAttentionPort
{
    Task<bool> IsLinkedToAppointmentAsync(
        Guid attentionId,
        Guid appointmentId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default);
}