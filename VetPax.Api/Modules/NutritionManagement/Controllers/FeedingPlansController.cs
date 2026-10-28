
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.NutritionManagement.Resources;
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.UseCases;

namespace VetPax.Api.Modules.NutritionManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/feeding-plans")]
[Produces("application/json")]
public sealed class FeedingPlansController : ControllerBase
{
    public const string GetByIdRouteName = "GetFeedingPlanById";

    private readonly GetFeedingPlanHandler _get;
    private readonly UpdateFeedingPlanHandler _update;
    private readonly ConfigureFeedingRemindersHandler _reminders;
    private readonly DeactivateFeedingPlanHandler _deactivate;

    public FeedingPlansController(
        GetFeedingPlanHandler get,
        UpdateFeedingPlanHandler update,
        ConfigureFeedingRemindersHandler reminders,
        DeactivateFeedingPlanHandler deactivate)
    {
        _get = get;
        _update = update;
        _reminders = reminders;
        _deactivate = deactivate;
    }

    /// <summary>
    /// TS07 - Get a feeding plan and its revisions.
    /// </summary>
    [HttpGet("{planId:guid}", Name = GetByIdRouteName)]
    [ProducesResponseType(
        typeof(FeedingPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid planId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _get.HandleAsync(
                new GetFeedingPlanQuery(planId),
                cancellationToken);

            return Ok(FeedingPlanResponse.From(result));
        }
        catch (Exception ex)
            when (NutritionHttpErrors.IsExpected(ex))
        {
            return NutritionHttpErrors.ToActionResult(this, ex);
        }
    }

    /// <summary>
    /// US08 / TS07 - Update the prescribed feeding plan.
    /// </summary>
    [HttpPut("{planId:guid}")]
    [ProducesResponseType(
        typeof(FeedingPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid planId,
        [FromBody] UpdateFeedingPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = FeedingPlanRequestMapper.ToCommand(
                planId,
                request);

            var result = await _update.HandleAsync(
                command,
                cancellationToken);

            return Ok(FeedingPlanResponse.From(result));
        }
        catch (Exception ex)
            when (NutritionHttpErrors.IsExpected(ex))
        {
            return NutritionHttpErrors.ToActionResult(this, ex);
        }
    }

    /// <summary>
    /// US09 - Enable or disable feeding reminders.
    /// </summary>
    [HttpPatch("{planId:guid}/reminders")]
    [ProducesResponseType(
        typeof(FeedingPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigureRemindersAsync(
        [FromRoute] Guid planId,
        [FromBody] ConfigureFeedingRemindersRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.Enabled is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid reminder configuration",
                    detail: "The enabled field is required.");
            }

            var result = await _reminders.HandleAsync(
                new ConfigureFeedingRemindersCommand(
                    planId,
                    request.Enabled.Value),
                cancellationToken);

            return Ok(FeedingPlanResponse.From(result));
        }
        catch (Exception ex)
            when (NutritionHttpErrors.IsExpected(ex))
        {
            return NutritionHttpErrors.ToActionResult(this, ex);
        }
    }

    /// <summary>
    /// US08 - Deactivate a feeding plan.
    /// </summary>
    [HttpPatch("{planId:guid}/deactivate")]
    [ProducesResponseType(
        typeof(FeedingPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateAsync(
        [FromRoute] Guid planId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _deactivate.HandleAsync(
                new DeactivateFeedingPlanCommand(planId),
                cancellationToken);

            return Ok(FeedingPlanResponse.From(result));
        }
        catch (Exception ex)
            when (NutritionHttpErrors.IsExpected(ex))
        {
            return NutritionHttpErrors.ToActionResult(this, ex);
        }
    }
}
