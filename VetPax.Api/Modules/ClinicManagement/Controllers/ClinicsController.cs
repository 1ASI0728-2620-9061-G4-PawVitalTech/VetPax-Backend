
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.ClinicManagement.Resources;
using VetPax.Application.Modules.ClinicManagement.Models;
using VetPax.Application.Modules.ClinicManagement.UseCases.GetClinicProfile;
using VetPax.Application.Modules.ClinicManagement.UseCases.UpdateClinicProfile;

namespace VetPax.Api.Modules.ClinicManagement.Controllers;

[ApiController]
[Route("api/v1/clinics")]
[Authorize]
public sealed class ClinicsController : ControllerBase
{
    private readonly GetClinicProfileHandler _get;
    private readonly UpdateClinicProfileHandler _update;

    public ClinicsController(
        GetClinicProfileHandler get,
        UpdateClinicProfileHandler update)
    {
        _get = get;
        _update = update;
    }

    [HttpGet("{clinicId:guid}")]
    [Authorize(Roles = "veterinarian,clinic_administrator")]
    public async Task<IActionResult> GetById(
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _get.HandleAsync(
                new GetClinicProfileQuery(clinicId),
                cancellationToken);

            return Ok(ClinicProfileResponse.From(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Clinic not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{clinicId:guid}")]
    [Authorize(Roles = "clinic_administrator")]
    public async Task<IActionResult> Update(
        Guid clinicId,
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
            var command = new UpdateClinicProfileCommand(
                clinicId,
                request.Name,
                request.Address,
                request.Phone,
                request.ContactEmail,
                request.OpeningHours.Select(h =>
                    new DailyHoursData(
                        h.DayOfWeek,
                        h.OpenTime,
                        h.CloseTime)).ToArray());

            var result = await _update.HandleAsync(
                command,
                cancellationToken);

            return Ok(ClinicProfileResponse.From(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Clinic not found." });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
