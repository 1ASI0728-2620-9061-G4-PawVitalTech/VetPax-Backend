using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.ReadModels;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

public sealed class ClinicPatientsAdapter
    : IClinicPatientsProvider
{
    public Task<IReadOnlyList<PatientSummary>> FindByClinicAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "PetClinicalCare patient-clinic association " +
            "is not implemented yet.");
    }
}