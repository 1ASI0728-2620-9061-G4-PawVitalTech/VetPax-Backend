using VetPax.Domain.Modules.ClinicManagement.ReadModels;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Application.Modules.ClinicManagement.Ports;

public interface IClinicPatientsProvider
{
    Task<IReadOnlyList<PatientSummary>> FindByClinicAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default);
}