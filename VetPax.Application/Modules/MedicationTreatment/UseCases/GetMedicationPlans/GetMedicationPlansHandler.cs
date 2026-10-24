
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.Ports;
using VetPax.Application.Modules.MedicationTreatment.Services;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.MedicationTreatment.Repositories;
using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlans;

public sealed class GetMedicationPlansHandler
{
    private readonly IMedicationPlanRepository _repository;
    private readonly IMedicationPatientPort _patients;
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly MedicationAuthorizationService _authorization;

    public GetMedicationPlansHandler(
        IMedicationPlanRepository repository,
        IMedicationPatientPort patients,
        IAuthenticatedUserPort currentUser,
        MedicationAuthorizationService authorization)
    {
        _repository = repository;
        _patients = patients;
        _currentUser = currentUser;
        _authorization = authorization;
    }

    public async Task<IReadOnlyList<MedicationPlanData>> HandleAsync(
        GetMedicationPlansQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Authentication required.");
        }

        var ownerId = await _patients.GetOwnerIdAsync(
            query.PetId,
            cancellationToken);

        if (!ownerId.HasValue)
            throw new KeyNotFoundException("Pet not found.");

        var plans = await _repository.ListByPetAsync(
            new MedicationPetId(query.PetId),
            cancellationToken);

        if (identity.Role == UserRole.Owner)
        {
            await _authorization.RequireOwnerAsync(
                query.PetId,
                cancellationToken);

            return plans
                .Where(x => x.OwnerId.Value == identity.UserId.Value)
                .Select(MedicationPlanData.From)
                .ToArray();
        }

        if (identity.Role == UserRole.Veterinarian)
        {
            var ownPrescriptions = plans
                .Where(x =>
                    x.VeterinarianId.Value ==
                    identity.UserId.Value)
                .ToArray();

            foreach (var plan in ownPrescriptions)
            {
                await _authorization.EnsureCanReadAsync(
                    plan,
                    cancellationToken);
            }

            return ownPrescriptions
                .Select(MedicationPlanData.From)
                .ToArray();
        }

        throw new UnauthorizedAccessException(
            "Insufficient permissions.");
    }
}
