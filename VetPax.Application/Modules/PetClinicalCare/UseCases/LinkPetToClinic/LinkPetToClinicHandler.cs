using VetPax.Application.Common.Ports;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Application.Modules.PetClinicalCare.UseCases.LinkPetToClinic;

public sealed record LinkPetToClinicCommand(
    Guid PetId,
    Guid ClinicId);

public sealed class LinkPetToClinicHandler(
    IAuthenticatedUserPort currentUser,
    IPetRepository pets,
    IClinicDirectoryPort clinics,
    IPetClinicLinkPort links,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(
        LinkPetToClinicCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PetId == Guid.Empty ||
            command.ClinicId == Guid.Empty)
            throw new ArgumentException("Invalid identifiers.");

        var identity = await currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null ||
            identity.Role != UserRole.Owner)
            throw new UnauthorizedAccessException();

        var pet = await pets.FindByIdAsync(
                      new PetId(command.PetId),
                      cancellationToken)
                  ?? throw new KeyNotFoundException("Pet not found.");

        if (!pet.IsOwnedBy(new OwnerId(identity.UserId.Value)))
            throw new UnauthorizedAccessException();

        if (!await clinics.ExistsAsync(
                command.ClinicId, cancellationToken))
            throw new KeyNotFoundException("Clinic not found.");

        await links.LinkAsync(
            new PetId(command.PetId),
            command.ClinicId,
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}