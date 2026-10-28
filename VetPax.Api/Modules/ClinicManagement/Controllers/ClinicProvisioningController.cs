
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.ClinicManagement.Resources;
using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Application.Modules.ClinicManagement.UseCases.AssignVeterinarian;
using VetPax.Application.Modules.ClinicManagement.UseCases.ProvisionClinic;

namespace VetPax.Api.Modules.ClinicManagement.Controllers;

public sealed record AssignVeterinarianRequest(
    Guid VeterinarianId);

[ApiController]
[Route("api/v1/clinics")]
[Authorize(Roles = "clinic_administrator")]
public sealed class ClinicProvisioningController(
    ProvisionClinicHandler provision,
    AssignVeterinarianHandler assign) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] UpdateClinicProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OpeningHours is null)
            return BadRequest(new
            {
                message = "Opening hours are required."
            });

        try
        {
            var result = await provision.HandleAsync(
                new ProvisionClinicCommand(
                    request.Name,
                    request.Address,
                    request.Phone,
                    request.ContactEmail,
                    request.OpeningHours.Select(h =>
                        new DailyHoursData(
                            h.DayOfWeek,
                            h.OpenTime,
                            h.CloseTime)).ToArray()),
                cancellationToken);

            return Created(
                $"/api/v1/clinics/{result.ClinicId}",
                ClinicProfileResponse.From(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{clinicId:guid}/veterinarians")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkVeterinarian(
        Guid clinicId,
        [FromBody] AssignVeterinarianRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await assign.HandleAsync(
                new AssignVeterinarianCommand(
                    clinicId,
                    request.VeterinarianId),
                cancellationToken);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Clinic not found."
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
