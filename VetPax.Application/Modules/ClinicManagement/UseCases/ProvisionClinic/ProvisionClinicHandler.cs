
using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.ProvisionClinic;

public sealed record ProvisionClinicCommand(
    string Name,
    string Address,
    string? Phone,
    string? ContactEmail,
    IReadOnlyList<DailyHoursData> OpeningHours);

public sealed class ProvisionClinicHandler(
    IAuthenticatedUserPort currentUser,
    IClinicRepository clinics,
    IUnitOfWork unitOfWork)
{
    public async Task<ClinicProfileData> HandleAsync(
        ProvisionClinicCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.OpeningHours);

        var identity = await currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty ||
            identity.Role != UserRole.ClinicAdministrator)
        {
            throw new UnauthorizedAccessException(
                "Clinic administrator required.");
        }

        var data = new ClinicGeneralData(
            command.Name,
            command.Address,
            command.Phone,
            command.ContactEmail);

        var schedule = new OpeningSchedule(
            command.OpeningHours.Select(h =>
                new DailyOpeningHours(
                    h.DayOfWeek,
                    h.OpenTime,
                    h.CloseTime)));

        var clinic = new Clinic(
            ClinicId.New(),
            new AdministratorId(identity.UserId.Value),
            data,
            schedule);

        await clinics.SaveAsync(clinic, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return ClinicProfileData.From(clinic);
    }
}
