using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Domain.Modules.AppointmentManagement.ReadModels;

public sealed record AgendaAppointmentEntry(
    AppointmentId AppointmentId,
    AppointmentPetId PetId,
    AppointmentClinicId ClinicId,
    AppointmentVeterinarianId VeterinarianId,
    AppointmentSlot Slot,
    string Reason,
    AppointmentStatus Status,
    Guid? ClinicalAttentionId);