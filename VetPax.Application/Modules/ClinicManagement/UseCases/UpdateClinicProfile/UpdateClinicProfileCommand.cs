using VetPax.Application.Modules.ClinicManagement.Models;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.UpdateClinicProfile;

public sealed record UpdateClinicProfileCommand(
    Guid ClinicId,
    string Name,
    string Address,
    string? Phone,
    string? ContactEmail,
    IReadOnlyList<DailyHoursData> OpeningHours
);