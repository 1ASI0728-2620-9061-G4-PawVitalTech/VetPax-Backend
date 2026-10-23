using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.ClinicManagement.Resources;
using VetPax.Application.Modules.ClinicManagement.UseCases.GetClinicPatients;

namespace VetPax.Api.Modules.ClinicManagement.Controllers;

[ApiController]
[Authorize(Roles = "veterinarian")]
[Route("api/v1/clinics/{clinicId:guid}/patients")]
public sealed class ClinicPatientsController(
    GetClinicPatientsHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPatients(
        Guid clinicId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(
                new GetClinicPatientsQuery(clinicId),
                cancellationToken);

            return Ok(new ClinicPatientsResponse(
                clinicId,
                result.HasPatients
                    ? null
                    : "No patients are linked to this clinic.",
                result.Patients
                    .Select(ClinicPatientResource.From)
                    .ToArray()));
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