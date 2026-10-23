using VetPax.Domain.Modules.ClinicManagement.Aggregates;

namespace VetPax.Application.Modules.ClinicManagement.Models;

public sealed record DailyHoursData(
    DayOfWeek DayOfWeek,
    TimeOnly OpenTime,
    TimeOnly CloseTime
);

public sealed record ClinicProfileData(
    Guid ClinicId,
    string Name,
    string Address,
    string? Phone,
    string? ContactEmail,
    IReadOnlyList<DailyHoursData> OpeningHours,
    DateTime UpdatedAt
)
{
    public static ClinicProfileData From(Clinic clinic)
    {
        return new ClinicProfileData(
            clinic.ClinicId.Value,
            clinic.GeneralData.Name,
            clinic.GeneralData.Address,
            clinic.GeneralData.Phone,
            clinic.GeneralData.ContactEmail,
            clinic.OpeningSchedule.Days
                .Select(d => new DailyHoursData(
                    d.DayOfWeek,
                    d.OpenTime,
                    d.CloseTime))
                .ToArray(),
            clinic.UpdatedAt);
    }
}