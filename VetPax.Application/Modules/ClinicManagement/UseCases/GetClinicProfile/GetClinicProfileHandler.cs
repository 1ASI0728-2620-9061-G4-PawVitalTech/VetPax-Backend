using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Application.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.GetClinicProfile;

public sealed class GetClinicProfileHandler
{
    private readonly IClinicRepository _clinics;
    private readonly ClinicRequesterResolver _requesters;
    private readonly ClinicAccessPolicy _policy;

    public GetClinicProfileHandler(
        IClinicRepository clinics,
        ClinicRequesterResolver requesters,
        ClinicAccessPolicy policy)
    {
        _clinics = clinics;
        _requesters = requesters;
        _policy = policy;
    }

    public async Task<ClinicProfileData> HandleAsync(
        GetClinicProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ClinicId == Guid.Empty)
            throw new ArgumentException("Invalid clinic ID.");

        var clinicId = new ClinicId(query.ClinicId);

        var clinic = await _clinics.FindByIdAsync(
                         clinicId, cancellationToken)
                     ?? throw new KeyNotFoundException(
                         "Clinic not found.");

        var requester = await _requesters.ResolveAsync(
            clinic, cancellationToken);

        if (!_policy.CanViewProfile(requester, clinicId))
            throw new UnauthorizedAccessException(
                "Access denied.");

        return ClinicProfileData.From(clinic);
    }
}