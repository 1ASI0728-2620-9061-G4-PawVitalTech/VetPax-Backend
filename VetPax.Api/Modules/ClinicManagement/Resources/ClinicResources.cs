using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Domain.Modules.ClinicManagement.ReadModels;

namespace VetPax.Api.Modules.ClinicManagement.Resources;

public sealed record OpeningHoursResource(
    DayOfWeek DayOfWeek,
    TimeOnly OpenTime,
    TimeOnly CloseTime);

public sealed record UpdateClinicProfileRequest(
    string Name,
    string Address,
    string? Phone,
    string? ContactEmail,
    IReadOnlyList<OpeningHoursResource>? OpeningHours);

public sealed record ClinicProfileResponse(
    Guid ClinicId,
    string Name,
    string Address,
    string? Phone,
    string? ContactEmail,
    IReadOnlyList<OpeningHoursResource> OpeningHours,
    DateTime UpdatedAt)
{
    public static ClinicProfileResponse From(ClinicProfileData data) =>
        new(
            data.ClinicId,
            data.Name,
            data.Address,
            data.Phone,
            data.ContactEmail,
            data.OpeningHours.Select(h =>
                new OpeningHoursResource(
                    h.DayOfWeek,
                    h.OpenTime,
                    h.CloseTime)).ToArray(),
            data.UpdatedAt);
}

public sealed record ClinicPatientResource(
    Guid PetId,
    string Name,
    string Species,
    DateTime? LastAttentionDate)
{
    public static ClinicPatientResource From(PatientSummary patient) =>
        new(
            patient.PetId,
            patient.Name,
            patient.Species,
            patient.LastAttentionDate);
}

public sealed record ClinicPatientsResponse(
    Guid ClinicId,
    string? Message,
    IReadOnlyList<ClinicPatientResource> Patients);