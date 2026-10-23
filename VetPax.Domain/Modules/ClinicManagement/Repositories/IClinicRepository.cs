using VetPax.Domain.Modules.ClinicManagement.Aggregates;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Domain.Modules.ClinicManagement.Repositories;

public interface IClinicRepository
{
    Task SaveAsync(
        Clinic clinic,
        CancellationToken cancellationToken = default);

    Task<Clinic?> FindByIdAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(
        ClinicId clinicId,
        CancellationToken cancellationToken = default);
}