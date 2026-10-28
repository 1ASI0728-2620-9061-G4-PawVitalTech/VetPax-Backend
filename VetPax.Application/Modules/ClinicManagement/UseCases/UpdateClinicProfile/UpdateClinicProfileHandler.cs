using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Application.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.UpdateClinicProfile;

public sealed class UpdateClinicProfileHandler
{
    private readonly IClinicRepository _clinics;
    private readonly ClinicRequesterResolver _requesters;
    private readonly ClinicAccessPolicy _policy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _events;

    public UpdateClinicProfileHandler(
        IClinicRepository clinics,
        ClinicRequesterResolver requesters,
        ClinicAccessPolicy policy,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher events)
    {
        _clinics = clinics;
        _requesters = requesters;
        _policy = policy;
        _unitOfWork = unitOfWork;
        _events = events;
    }

    public async Task<ClinicProfileData> HandleAsync(
        UpdateClinicProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ClinicId == Guid.Empty)
            throw new ArgumentException("Invalid clinic ID.");

        var clinic = await _clinics.FindByIdAsync(
            new ClinicId(command.ClinicId),
            cancellationToken)
            ?? throw new KeyNotFoundException(
                "Clinic not found.");

        var requester = await _requesters.ResolveAsync(
            clinic, cancellationToken);

        if (!_policy.CanManageProfile(requester, clinic))
            throw new UnauthorizedAccessException(
                "Only a clinic administrator can update the profile.");

        ArgumentNullException.ThrowIfNull(command.OpeningHours);

        var generalData = new ClinicGeneralData(
            command.Name,
            command.Address,
            command.Phone,
            command.ContactEmail);

        var schedule = new OpeningSchedule(
            command.OpeningHours.Select(d =>
                new DailyOpeningHours(
                    d.DayOfWeek,
                    d.OpenTime,
                    d.CloseTime)));

        clinic.UpdateProfile(generalData, schedule);

        await _clinics.SaveAsync(clinic, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        await _events.PublishAsync(
            clinic.PullDomainEvents(),
            cancellationToken);

        return ClinicProfileData.From(clinic);
    }
}
