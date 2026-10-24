
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Domain.Modules.MedicationTreatment.Aggregates;
using VetPax.Domain.Modules.MedicationTreatment.Entities;
using VetPax.Domain.Shared.Events;

namespace VetPax.Application.Modules.MedicationTreatment.Ports;

// PetClinicalCare: consulta mínima sobre la mascota.
public interface IMedicationPatientPort
{
    Task<Guid?> GetOwnerIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default);
}

// ClinicManagement: autorización clínica.
public interface IMedicationVeterinarianPort
{
    Task<bool> CanPrescribeAsync(
        Guid veterinarianId,
        Guid clinicId,
        Guid petId,
        CancellationToken cancellationToken = default);
}

// US03: comprobación opcional de atención clínica.
public interface IMedicationClinicalContextPort
{
    Task<bool> MatchesAsync(
        Guid clinicalAttentionId,
        Guid petId,
        Guid clinicId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default);
}

// Persistencia de avisos pendientes.
// La implementación debe compartir el UnitOfWork.
public interface IMedicationReminderPort
{
    Task StageForDoseAsync(
        MedicationPlan plan,
        MedicationDose dose,
        CancellationToken cancellationToken = default);

    Task CancelForDoseAsync(
        Guid doseId,
        CancellationToken cancellationToken = default);

    Task CancelPendingForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default);
}

// Escritura transaccional de eventos.
// Este puerto NO publica directamente en un broker.
public interface IMedicationOutboxPort
{
    Task StageAsync(
        IReadOnlyCollection<IDomainEvent> events,
        CancellationToken cancellationToken = default);
}

// Adaptador de envío para TS06.
// Será utilizado por Infrastructure, no por los
// handlers que modifican tratamientos.
public interface IMedicationNotificationSender
{
    Task SendAsync(
        MedicationNotificationMessage message,
        CancellationToken cancellationToken = default);
}
