
using VetPax.Domain.Modules.AppointmentManagement.Aggregates;
using VetPax.Domain.Modules.AppointmentManagement.Enums;
using VetPax.Domain.Modules.AppointmentManagement.ReadModels;

namespace VetPax.Application.Modules.AppointmentManagement.Models;

public sealed record AppointmentData(
    Guid AppointmentId,
    Guid OwnerId,
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Reason,
    AppointmentStatus Status,
    Guid? ClinicalAttentionId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static AppointmentData From(Appointment appointment)
    {
        return new AppointmentData(
            appointment.AppointmentId.Value,
            appointment.OwnerId.Value,
            appointment.PetId.Value,
            appointment.ClinicId.Value,
            appointment.VeterinarianId.Value,
            appointment.Slot.Date,
            appointment.Slot.StartTime,
            appointment.Slot.EndTime,
            appointment.Reason,
            appointment.Status,
            appointment.ClinicalAttentionId,
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }
}

public sealed record AgendaAppointmentData(
    Guid AppointmentId,
    Guid PetId,
    string? PetName,
    string? Species,
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Reason,
    AppointmentStatus Status,
    Guid? ClinicalAttentionId)
{
    public static AgendaAppointmentData From(
        AgendaAppointmentEntry entry,
        AppointmentPatientSummary? patient)
    {
        return new AgendaAppointmentData(
            entry.AppointmentId.Value,
            entry.PetId.Value,
            patient?.Name,
            patient?.Species,
            entry.ClinicId.Value,
            entry.VeterinarianId.Value,
            entry.Slot.Date,
            entry.Slot.StartTime,
            entry.Slot.EndTime,
            entry.Reason,
            entry.Status,
            entry.ClinicalAttentionId);
    }
}

public sealed record AppointmentPatientSummary(
    Guid PetId,
    string Name,
    string Species);
