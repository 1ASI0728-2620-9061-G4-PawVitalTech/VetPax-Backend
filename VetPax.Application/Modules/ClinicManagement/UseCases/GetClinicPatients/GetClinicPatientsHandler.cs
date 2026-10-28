using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Application.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.ReadModels;
using VetPax.Domain.Modules.ClinicManagement.Repositories;
using VetPax.Domain.Modules.ClinicManagement.Services;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.GetClinicPatients;

public sealed class GetClinicPatientsHandler
{
    private readonly IClinicRepository _clinics;
    private readonly IClinicPatientsProvider _patients;
    private readonly ClinicRequesterResolver _requesters;
    private readonly ClinicAccessPolicy _policy;

    public GetClinicPatientsHandler(
        IClinicRepository clinics,
        IClinicPatientsProvider patients,
        ClinicRequesterResolver requesters,
        ClinicAccessPolicy policy)
    {
        _clinics = clinics;
        _patients = patients;
        _requesters = requesters;
        _policy = policy;
    }

    public async Task<PatientListView> HandleAsync(
        GetClinicPatientsQuery query,
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

        if (!_policy.CanViewPatients(requester, clinicId))
            throw new UnauthorizedAccessException(
                "Access denied.");

        var patients = await _patients.FindByClinicAsync(
            clinicId, cancellationToken);

        return new PatientListView(clinicId, patients);
    }
}