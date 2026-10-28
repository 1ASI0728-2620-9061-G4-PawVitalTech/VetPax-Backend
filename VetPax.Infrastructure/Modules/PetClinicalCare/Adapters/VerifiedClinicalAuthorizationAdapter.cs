
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

public sealed class VerifiedClinicalAuthorizationAdapter
    : IClinicalAuthorizationPort
{
    private readonly IClinicalRelationshipPort _relationships;
    private readonly VeterinaryClinicalAccessPolicy _policy;

    public VerifiedClinicalAuthorizationAdapter(
        IClinicalRelationshipPort relationships,
        VeterinaryClinicalAccessPolicy policy)
    {
        _relationships = relationships;
        _policy = policy;
    }

    public async Task<VeterinarianId?> GetAuthorizedVeterinarianAsync(
        Guid userId,
        ClinicId clinicId,
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        var requester = new AccessRequester(
            userId,
            RequesterRole.Veterinarian,
            clinicId);

        var authorized = await IsAuthorizedAsync(
            petId,
            requester,
            cancellationToken);

        if (!authorized)
            return null;

        return new VeterinarianId(userId);
    }

    public Task<bool> CanViewEvolutionAsync(
        Guid userId,
        ClinicId clinicId,
        PetId petId,
        CancellationToken cancellationToken = default)
    {
        var requester = new AccessRequester(
            userId,
            RequesterRole.Veterinarian,
            clinicId);

        return IsAuthorizedAsync(
            petId,
            requester,
            cancellationToken);
    }

    public async Task<bool> IsAuthorizedAsync(
        PetId petId,
        AccessRequester requester,
        CancellationToken cancellationToken = default)
    {
        if (requester is null ||
            petId.Value == Guid.Empty ||
            !requester.IsVeterinarian() ||
            requester.UserId == Guid.Empty ||
            requester.ClinicId is not { } clinicId ||
            clinicId.Value == Guid.Empty)
        {
            return false;
        }

        var membership = await _relationships
            .HasActiveVeterinarianMembershipAsync(
                requester.UserId,
                clinicId.Value,
                cancellationToken);

        if (!membership)
            return false;

        var patientLink = await _relationships
            .HasActivePatientLinkAsync(
                petId.Value,
                clinicId.Value,
                cancellationToken);

        return _policy.CanAccess(
            requester,
            membership,
            patientLink);
    }
}
