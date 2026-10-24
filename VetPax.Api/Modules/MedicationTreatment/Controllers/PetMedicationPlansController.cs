
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.MedicationTreatment.Resources;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.UseCases.CreateMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlans;

namespace VetPax.Api.Modules.MedicationTreatment.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/pets/{petId:guid}/medication-plans")]
public sealed class PetMedicationPlansController : ControllerBase
{
    private readonly CreateMedicationPlanHandler _create;
    private readonly GetMedicationPlansHandler _getPlans;

    public PetMedicationPlansController(
        CreateMedicationPlanHandler create,
        GetMedicationPlansHandler getPlans)
    {
        _create = create;
        _getPlans = getPlans;
    }

    // ---------------------------------------------
    // POST /api/v1/pets/{petId}/medication-plans
    // ---------------------------------------------

    [HttpPost]
    [Authorize(Roles = "veterinarian")]
    [ProducesResponseType(
        typeof(MedicationPlanResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid petId,
        [FromBody] CreateMedicationPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(
                new { message = "Request body is required." });

        try
        {
            if (petId == Guid.Empty ||
                request.ClinicId == Guid.Empty)
            {
                return BadRequest(new
                {
                    message = "Valid pet and clinic IDs are required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.MedicationName) ||
                string.IsNullOrWhiteSpace(request.Route) ||
                string.IsNullOrWhiteSpace(request.TimeZoneId) ||
                request.StartDate == default)
            {
                return BadRequest(new
                {
                    message = "Medication name, route, start date " +
                              "and time zone are required."
                });
            }

            var schedules = MedicationRequestMapper.ToInputs(
                request.Schedules);

            var result = await _create.HandleAsync(
                new CreateMedicationPlanCommand(
                    petId,
                    request.ClinicId,
                    request.ClinicalAttentionId,
                    request.MedicationName,
                    request.Route,
                    request.Instructions ?? string.Empty,
                    request.StartDate,
                    request.EndDate,
                    request.TimeZoneId,
                    schedules),
                cancellationToken);

            var response = MedicationPlanResponse.From(result);

            return Created(
                $"/api/v1/medication-plans/{result.PlanId}",
                response);
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }

    // ---------------------------------------------
    // GET /api/v1/pets/{petId}/medication-plans
    // ---------------------------------------------

    [HttpGet]
    [Authorize(Roles = "owner,veterinarian")]
    [ProducesResponseType(
        typeof(MedicationPlanResponse[]),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPet(
        [FromRoute] Guid petId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getPlans.HandleAsync(
                new GetMedicationPlansQuery(petId),
                cancellationToken);

            return Ok(
                result.Select(MedicationPlanResponse.From)
                    .ToArray());
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }
}
