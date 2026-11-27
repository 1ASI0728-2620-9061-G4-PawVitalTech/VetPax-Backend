
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetMyPets;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/pets")]
public sealed class PetsController : ControllerBase
{
    private readonly RegisterPetHandler _registerPetHandler;
    private readonly GetPetHandler _getPetHandler;
    private readonly IAuthenticatedUserPort _currentUser;
    private readonly GetMyPetsHandler _getMyPetsHandler;

    public PetsController(
        RegisterPetHandler registerPetHandler,
        GetPetHandler getPetHandler,
        IAuthenticatedUserPort currentUser,
        GetMyPetsHandler getMyPetsHandler)
    {
        _registerPetHandler = registerPetHandler;
        _getPetHandler = getPetHandler;
        _currentUser = currentUser;
        _getMyPetsHandler = getMyPetsHandler;
    }

    [HttpPost]
    [Authorize(Roles = "owner")]
    [ProducesResponseType(
        typeof(RegisterPetResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPetRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var command = new RegisterPetCommand(
                new OwnerId(identity.UserId.Value),
                request.Name,
                request.Species,
                request.BirthDate,
                request.Breed,
                request.Sex,
                request.TutorName,
                request.MicrochipNumber,
                request.InsuranceProvider,
                request.PolicyNumber,
                request.PhotoUrl
            );

            var result = await _registerPetHandler.HandleAsync(
                command,
                cancellationToken
            );

            var response = new RegisterPetResponse(
                result.PetId.Value,
                result.ClinicalRecordId.Value
            );

            return Created(
                $"/api/v1/pets/{response.PetId}",
                response
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
    [HttpGet]
    [Authorize(Roles = "owner")]
    [ProducesResponseType(
        typeof(IReadOnlyList<PetResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine(
        CancellationToken cancellationToken)
    {
        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        var result = await _getMyPetsHandler.HandleAsync(
            new OwnerId(identity.UserId.Value),
            cancellationToken);

        return Ok(result.Select(p => new PetResponse(
            p.PetId.Value,
            p.Name,
            p.Species.ToString(),
            p.BirthDate,
            p.Breed,
            p.Sex?.ToString(),
            p.RegisteredAt,
            p.TutorName,
            p.MicrochipNumber,
            p.InsuranceProvider,
            p.PolicyNumber,
            p.PhotoUrl
        )).ToArray());
    }

    [HttpGet("{petId:guid}")]
    [Authorize(Roles = "owner,veterinarian")]
    [ProducesResponseType(
        typeof(PetResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid petId,
        CancellationToken cancellationToken)
    {
        if (petId == Guid.Empty)
            return BadRequest(new
            {
                message = "A valid pet ID is required."
            });

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var result = identity.Role switch
            {
                UserRole.Owner =>
                    await _getPetHandler.HandleAsync(
                        new GetPetQuery(
                            new PetId(petId),
                            new OwnerId(identity.UserId.Value)),
                        cancellationToken),
                UserRole.Veterinarian =>
                    await _getPetHandler.HandleForVeterinarianAsync(
                        new PetId(petId),
                        identity.UserId.Value,
                        cancellationToken),
                _ => null
            };

            if (identity.Role is not
                (UserRole.Owner or UserRole.Veterinarian))
                return Forbid();

            if (result is null)
                return NotFound(new
                {
                    message = "Pet not found."
                });

            return Ok(new PetResponse(
                result.PetId.Value,
                result.Name,
                result.Species.ToString(),
                result.BirthDate,
                result.Breed,
                result.Sex?.ToString(),
                result.RegisteredAt,
                result.TutorName,
                result.MicrochipNumber,
                result.InsuranceProvider,
                result.PolicyNumber,
                result.PhotoUrl
            ));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new
            {
                message = "Access denied."
            });
        }
    }
}
