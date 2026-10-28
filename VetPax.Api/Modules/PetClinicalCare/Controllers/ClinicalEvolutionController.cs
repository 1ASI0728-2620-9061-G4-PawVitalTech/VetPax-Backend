
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.UseCases.GetClinicalEvolution;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Authorize(Roles = "veterinarian")]
[Route("api/v1/pets/{petId:guid}/evolution")]
public sealed class ClinicalEvolutionController : ControllerBase
{
    private readonly GetClinicalEvolutionHandler _handler;
    private readonly IAuthenticatedUserPort _currentUser;

    public ClinicalEvolutionController(
        GetClinicalEvolutionHandler handler,
        IAuthenticatedUserPort currentUser)
    {
        _handler = handler;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvolution(
        Guid petId,
        [FromQuery] Guid clinicId,
        CancellationToken cancellationToken)
    {
        if (petId == Guid.Empty || clinicId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Valid pet and clinic IDs are required."
            });
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var requester = new AccessRequester(
                identity.UserId.Value,
                RequesterRole.Veterinarian,
                new ClinicId(clinicId)
            );

            var query = new GetClinicalEvolutionQuery(
                new PetId(petId),
                requester
            );

            var result = await _handler.HandleAsync(
                query,
                cancellationToken
            );

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new
            {
                message = "Access denied."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
