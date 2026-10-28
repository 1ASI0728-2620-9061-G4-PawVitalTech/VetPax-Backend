using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.ClinicManagement.ReadModels;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

public sealed class ClinicPatientsAdapter(
    IPetClinicPatientReadPort clinicalCare)
    : IClinicPatientsProvider
{
    public async Task<IReadOnlyList<PatientSummary>> FindByClinicAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default)
    {
        var patients = await clinicalCare.FindByClinicAsync(
            clinicId.Value,
            cancellationToken);

        return patients.Select(p => new PatientSummary(
            p.PetId,
            p.Name,
            p.Species,
            p.LastAttentionDate
        )).ToArray();
    }
}