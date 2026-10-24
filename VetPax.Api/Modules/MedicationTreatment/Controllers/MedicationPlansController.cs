
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.MedicationTreatment.Resources;
using VetPax.Application.Modules.MedicationTreatment.Models;
using VetPax.Application.Modules.MedicationTreatment.UseCases.ConfigureMedicationReminders;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationDoses;
using VetPax.Application.Modules.MedicationTreatment.UseCases.GetMedicationPlan;
using VetPax.Application.Modules.MedicationTreatment.UseCases.UpdateMedicationPlan;

namespace VetPax.Api.Modules.MedicationTreatment.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/medication-plans")]
public sealed class MedicationPlansController : ControllerBase
{
    private readonly GetMedicationPlanHandler _getPlan;
    private readonly UpdateMedicationPlanHandler _update;
    private readonly ConfigureMedicationRemindersHandler _reminders;
    private readonly GetMedicationDosesHandler _getDoses;

    public MedicationPlansController(
        GetMedicationPlanHandler getPlan,
        UpdateMedicationPlanHandler update,
        ConfigureMedicationRemindersHandler reminders,
        GetMedicationDosesHandler getDoses)
    {
        _getPlan = getPlan;
        _update = update;
        _reminders = reminders;
        _getDoses = getDoses;
    }

    // ---------------------------------------------
    // GET /api/v1/medication-plans/{planId}
    // ---------------------------------------------

    [HttpGet("{planId:guid}")]
    [Authorize(Roles = "owner,veterinarian")]
    [ProducesResponseType(
        typeof(MedicationPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid planId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getPlan.HandleAsync(
                new GetMedicationPlanQuery(planId),
                cancellationToken);

            return Ok(MedicationPlanResponse.From(result));
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }

    // ---------------------------------------------
    // PATCH /api/v1/medication-plans/{planId}
    // ---------------------------------------------

    [HttpPatch("{planId:guid}")]
    [Authorize(Roles = "veterinarian")]
    [ProducesResponseType(
        typeof(MedicationPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid planId,
        [FromBody] UpdateMedicationPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(
                new { message = "Request body is required." });

        try
        {
            var operation =
                MedicationRequestMapper.ParseOperation(
                    request.Operation);

            IReadOnlyList<MedicationScheduleInput>? schedules = null;

            if (operation == MedicationUpdateOperation.ChangeSchedules)
            {
                schedules = MedicationRequestMapper.ToInputs(
                    request.Schedules);
            }
            else if (request.Schedules is not null)
            {
                return BadRequest(new
                {
                    message = "Schedules are only valid " +
                              "for changeSchedules."
                });
            }

            var result = await _update.HandleAsync(
                new UpdateMedicationPlanCommand(
                    planId,
                    operation,
                    schedules),
                cancellationToken);

            return Ok(MedicationPlanResponse.From(result));
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }

    // ---------------------------------------------
    // PATCH /api/v1/medication-plans/{planId}/reminders
    // ---------------------------------------------

    [HttpPatch("{planId:guid}/reminders")]
    [Authorize(Roles = "owner")]
    [ProducesResponseType(
        typeof(MedicationPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfigureReminders(
        [FromRoute] Guid planId,
        [FromBody] ConfigureMedicationRemindersRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(
                new { message = "Request body is required." });

        try
        {
            var result = await _reminders.HandleAsync(
                new ConfigureMedicationRemindersCommand(
                    planId,
                    request.Enabled),
                cancellationToken);

            return Ok(MedicationPlanResponse.From(result));
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }

    // ---------------------------------------------
    // GET /api/v1/medication-plans/{planId}/doses
    // ---------------------------------------------

    [HttpGet("{planId:guid}/doses")]
    [Authorize(Roles = "owner,veterinarian")]
    [ProducesResponseType(
        typeof(MedicationDoseResponse[]),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoses(
        [FromRoute] Guid planId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getDoses.HandleAsync(
                new GetMedicationDosesQuery(
                    planId,
                    from,
                    to),
                cancellationToken);

            return Ok(
                result.Select(MedicationDoseResponse.From)
                    .ToArray());
        }
        catch (Exception exception)
            when (MedicationHttpErrors.CanHandle(exception))
        {
            return MedicationHttpErrors.Map(this, exception);
        }
    }
}
