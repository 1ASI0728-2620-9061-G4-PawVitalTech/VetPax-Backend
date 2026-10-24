
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.MedicationTreatment.Resources;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.UseCases.RegisterDoseAdministration;

namespace VetPax.Api.Modules.MedicationTreatment.Controllers;

[ApiController]
[Authorize(Roles = "owner")]
[Route("api/v1/medication-doses")]
public sealed class MedicationDosesController : ControllerBase
{
    private readonly RegisterDoseAdministrationHandler _register;

    public MedicationDosesController(
        RegisterDoseAdministrationHandler register)
    {
        _register = register;
    }

    // ---------------------------------------------
    // POST /api/v1/medication-doses/{doseId}/administrations
    // ---------------------------------------------

    [HttpPost("{doseId:guid}/administrations")]
    [ProducesResponseType(
        typeof(MedicationAdministrationResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAdministration(
        [FromRoute] Guid doseId,
        [FromBody] RegisterMedicationAdministrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "Request body is required."
            });
        }

        if (doseId == Guid.Empty)
        {
            return BadRequest(new
            {
                message = "A valid medication dose ID is required."
            });
        }

        if (request.AdministeredAtUtc == default)
        {
            return BadRequest(new
            {
                message = "Administration time is required."
            });
        }

        try
        {
            // DateTimeOffset conserva correctamente
            // la zona horaria suministrada.
            //
            // Application recibe el instante en UTC.
            var administeredAtUtc =
                request.AdministeredAtUtc.UtcDateTime;

            var result = await _register.HandleAsync(
                new RegisterDoseAdministrationCommand(
                    doseId,
                    administeredAtUtc),
                cancellationToken);

            var response =
                MedicationAdministrationResponse.From(result);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }
}
