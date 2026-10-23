
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.PetClinicalCare.Services;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.AuthorizeVeterinaryAccess;

public sealed record AuthorizeVeterinaryAccessQuery(
    Guid PetId,
    Guid ClinicId);

public sealed class AuthorizeVeterinaryAccessHandler
{
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly IClinicalRelationshipPort _relationships;
    private readonly VeterinaryClinicalAccessPolicy _policy;

    public AuthorizeVeterinaryAccessHandler(
        IAuthenticatedUserPort currentUser,
        IClinicalRelationshipPort relationships,
        VeterinaryClinicalAccessPolicy policy)
    {
        _currentUser = currentUser;
        _relationships = relationships;
        _policy = policy;
    }

    public async Task<AccessRequester> HandleAsync(
        AuthorizeVeterinaryAccessQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PetId == Guid.Empty ||
            query.ClinicId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid pet and clinic identifiers are required.");
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
        {
            throw new UnauthorizedAccessException(
                "Authentication required.");
        }

        if (identity.Role != UserRole.Veterinarian)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian role required.");
        }

        var requester = new AccessRequester(
            identity.UserId.Value,
            RequesterRole.Veterinarian,
            new ClinicId(query.ClinicId));

        var isClinicMember =
            await _relationships
                .HasActiveVeterinarianMembershipAsync(
                    identity.UserId.Value,
                    query.ClinicId,
                    cancellationToken);

        if (!isClinicMember)
        {
            throw new UnauthorizedAccessException(
                "Veterinarian is not assigned to this clinic.");
        }

        var isPatientLinked =
            await _relationships.HasActivePatientLinkAsync(
                query.PetId,
                query.ClinicId,
                cancellationToken);

        if (!_policy.CanAccess(
                requester,
                isClinicMember,
                isPatientLinked))
        {
            throw new UnauthorizedAccessException(
                "Patient is not authorized for this clinic.");
        }

        return requester;
    }
}
