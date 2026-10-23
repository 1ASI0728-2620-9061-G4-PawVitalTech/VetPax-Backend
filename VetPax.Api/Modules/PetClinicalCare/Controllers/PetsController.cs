
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.PetClinicalCare.Resources;
using VetPax.Application.Modules.PetClinicalCare.UseCases.RegisterPet;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetPet;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Route("api/v1/pets")]
public sealed class PetsController : ControllerBase
{
    private readonly RegisterPetHandler _registerPetHandler;
    private readonly IWebHostEnvironment _environment;
    private readonly GetPetHandler _getPetHandler;


    public PetsController(
        RegisterPetHandler registerPetHandler,
        GetPetHandler getPetHandler,
        IWebHostEnvironment environment)
    {
        _registerPetHandler = registerPetHandler;
        _getPetHandler = getPetHandler;
        _environment = environment;
    }


    [HttpPost]
    [ProducesResponseType(
        typeof(RegisterPetResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPetRequest request,
        [FromHeader(Name = "X-Dev-Owner-Id")] string? devOwnerId,
        CancellationToken cancellationToken)
    {
        // Temporary authentication mechanism for local development
        if (!_environment.IsDevelopment())
        {
            return StatusCode(403, new
            {
                message = "Authentication integration is required."
            });
        }

        if (!Guid.TryParse(devOwnerId, out var ownerGuid)
            || ownerGuid == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid X-Dev-Owner-Id header is required."
            });
        }

        try
        {
            var command = new RegisterPetCommand(
                new OwnerId(ownerGuid),
                request.Name,
                request.Species,
                request.BirthDate,
                request.Breed,
                request.Sex
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
    
    
    [HttpGet("{petId:guid}")]
    [ProducesResponseType(
        typeof(PetResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid petId,
        [FromHeader(Name = "X-Dev-Owner-Id")] string? devOwnerId,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return StatusCode(403, new
            {
                message = "Authentication integration is required."
            });
        }

        if (petId == Guid.Empty)
            return BadRequest(new
            {
                message = "A valid pet ID is required."
            });

        if (!Guid.TryParse(devOwnerId, out var ownerGuid)
            || ownerGuid == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid X-Dev-Owner-Id header is required."
            });
        }

        try
        {
            var query = new GetPetQuery(
                new PetId(petId),
                new OwnerId(ownerGuid)
            );

            var result = await _getPetHandler.HandleAsync(
                query,
                cancellationToken
            );

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
                result.RegisteredAt
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
