
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.MedicationTreatment.Resources;
using VetPax.Application.Modules.MedicationTreatment
    .UseCases.GetClinicMedicationPlans;

namespace VetPax.Api.Modules.MedicationTreatment.Controllers;

[ApiController]
[Authorize(Roles = "veterinarian")]
[Route("api/v1/clinics/{clinicId:guid}/patients/{petId:guid}/medication-plans")]
public sealed class ClinicMedicationPlansController : ControllerBase
{
    private readonly GetClinicMedicationPlansHandler _handler;

    public ClinicMedicationPlansController(
        GetClinicMedicationPlansHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(MedicationPlanResponse[]),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlans(
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

        try
        {
            var plans = await _handler.HandleAsync(
                petId,
                clinicId,
                cancellationToken);

            return Ok(
                plans.Select(MedicationPlanResponse.From)
                    .ToArray());
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new
            {
                message = "Access denied."
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new
            {
                message = "Patient not found."
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
