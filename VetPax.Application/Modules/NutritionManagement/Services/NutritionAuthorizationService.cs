
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Domain.Modules.NutritionManagement.Aggregates;

namespace VetPax.Application.Modules.NutritionManagement.Services;

public sealed class NutritionAuthorizationService
{
    private readonly INutritionIdentityPort _identity;
    private readonly INutritionPatientPort _patients;
    private readonly INutritionVeterinarianPort _veterinarians;

    public NutritionAuthorizationService(
        INutritionIdentityPort identity,
        INutritionPatientPort patients,
        INutritionVeterinarianPort veterinarians)
    {
        _identity = identity;
        _patients = patients;
        _veterinarians = veterinarians;
    }

    public async Task<Guid> RequireVeterinarianAsync(
        Guid petId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActorAsync(cancellationToken);

        if (actor.Role != NutritionUserRole.Veterinarian)
            throw new UnauthorizedAccessException(
                "Only veterinarians can prescribe feeding plans.");

        var ownerId = await _patients.GetOwnerIdAsync(
            petId, cancellationToken);

        if (!ownerId.HasValue)
            throw new KeyNotFoundException("Pet not found.");

        if (!await _veterinarians.CanPrescribeAsync(
                actor.UserId, clinicId, petId, cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "The veterinarian is not authorized for this clinic and pet.");
        }

        return actor.UserId;
    }

    public async Task RequirePlanVeterinarianAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken = default)
    {
        var veterinarianId = await RequireVeterinarianAsync(
            plan.PetId,
            plan.ClinicId,
            cancellationToken);

        if (veterinarianId != plan.VeterinarianId)
            throw new UnauthorizedAccessException(
                "Only the prescribing veterinarian can modify this plan.");
    }

    public async Task RequireOwnerAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActorAsync(cancellationToken);

        if (actor.Role != NutritionUserRole.Owner)
            throw new UnauthorizedAccessException(
                "An owner account is required.");

        var ownerId = await _patients.GetOwnerIdAsync(
            plan.PetId,
            cancellationToken);

        if (!ownerId.HasValue ||
            ownerId.Value != actor.UserId)
        {
            throw new UnauthorizedAccessException(
                "The feeding plan does not belong to this owner.");
        }
    }

    public async Task RequireReadAccessAsync(
        FeedingPlan plan,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActorAsync(cancellationToken);

        if (actor.Role == NutritionUserRole.Owner)
        {
            await RequireOwnerAsync(plan, cancellationToken);
            return;
        }

        if (actor.Role == NutritionUserRole.Veterinarian)
        {
            if (!await _veterinarians.CanPrescribeAsync(
                    actor.UserId,
                    plan.ClinicId,
                    plan.PetId,
                    cancellationToken))
            {
                throw new UnauthorizedAccessException(
                    "The veterinarian cannot access this feeding plan.");
            }

            return;
        }

        throw new UnauthorizedAccessException(
            "The current user cannot access feeding plans.");
    }

    private async Task<NutritionActor> RequireActorAsync(
        CancellationToken cancellationToken)
    {
        var actor = await _identity.GetCurrentAsync(cancellationToken);

        if (actor is null || actor.UserId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "An authenticated user is required.");

        return actor;
    }
}
