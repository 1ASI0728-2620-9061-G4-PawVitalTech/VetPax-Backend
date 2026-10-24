
using VetPax.Application.Modules.AppointmentManagement.Models;

namespace VetPax.Api.Modules.AppointmentManagement.Resources;

// US04: registrar una cita.
public sealed record ScheduleAppointmentRequest(
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date,
    TimeOnly StartTime,
    string Reason);

// US05 / US06: modificar el estado o el horario.
public sealed record PatchAppointmentRequest(
    string? Operation,
    DateOnly? Date,
    TimeOnly? StartTime,
    Guid? ClinicalAttentionId);

// Respuesta individual.
public sealed record AppointmentResponse(
    Guid AppointmentId,
    Guid OwnerId,
    Guid PetId,
    Guid ClinicId,
    Guid VeterinarianId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Reason,
    string Status,
    Guid? ClinicalAttentionId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static AppointmentResponse From(
        AppointmentData data)
    {
        return new AppointmentResponse(
            data.AppointmentId,
            data.OwnerId,
            data.PetId,
            data.ClinicId,
            data.VeterinarianId,
            data.Date,
            data.StartTime,
            data.EndTime,
            data.Reason,
            data.Status.ToString(),
            data.ClinicalAttentionId,
            data.CreatedAt,
            data.UpdatedAt
        );
    }
}

// Una entrada del listado de agenda.
public sealed record AgendaAppointmentResponse(
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
    string Status,
    Guid? ClinicalAttentionId)
{
    public static AgendaAppointmentResponse From(
        AgendaAppointmentData data)
    {
        return new AgendaAppointmentResponse(
            data.AppointmentId,
            data.PetId,
            data.PetName,
            data.Species,
            data.ClinicId,
            data.VeterinarianId,
            data.Date,
            data.StartTime,
            data.EndTime,
            data.Reason,
            data.Status.ToString(),
            data.ClinicalAttentionId
        );
    }
}

// Respuesta de consulta por período.
public sealed record AppointmentAgendaResponse(
    Guid ClinicId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<AgendaAppointmentResponse> Appointments);
