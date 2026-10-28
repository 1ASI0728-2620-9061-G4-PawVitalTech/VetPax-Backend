using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Application.Modules.PetClinicalCare.UseCases.LinkPetToClinic;

namespace VetPax.Api.Modules.PetClinicalCare.Controllers;

[ApiController]
[Authorize(Roles = "owner")]
[Route("api/v1/pets/{petId:guid}/clinics")]
public sealed class PetClinicLinksController(
    LinkPetToClinicHandler handler) : ControllerBase
{
    [HttpPut("{clinicId:guid}")]
    public async Task<IActionResult> Link(
        Guid petId,
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        try
        {
            await handler.HandleAsync(
                new LinkPetToClinicCommand(petId, clinicId),
                cancellationToken);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}