
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare
    .UseCases.GetVeterinarianPatient;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Authorize(Roles = "veterinarian")]
[Route("api/v1/clinics/{clinicId:guid}/patients/{petId:guid}")]
public sealed class VeterinarianPatientController : ControllerBase
{
    private readonly GetVeterinarianPatientHandler _handler;
    private readonly IAuthenticatedUserPort _currentUser;

    public VeterinarianPatientController(
        GetVeterinarianPatientHandler handler,
        IAuthenticatedUserPort currentUser)
    {
        _handler = handler;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(GetVeterinarianPatientResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatient(
        Guid clinicId,
        Guid petId,
        CancellationToken cancellationToken)
    {
        if (clinicId == Guid.Empty || petId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "Valid clinic and pet IDs are required."
            });
        }

        var identity = await _currentUser.GetCurrentAsync(
            cancellationToken);

        if (identity is null)
            return Unauthorized();

        try
        {
            var result = await _handler.HandleAsync(
                identity.UserId.Value,
                clinicId,
                petId,
                cancellationToken);

            if (result is null)
                return NotFound(new
                {
                    message = "Pet not found."
                });

            return Ok(result);
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
