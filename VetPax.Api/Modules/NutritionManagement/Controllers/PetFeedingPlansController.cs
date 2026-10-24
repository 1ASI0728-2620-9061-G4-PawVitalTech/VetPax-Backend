
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.NutritionManagement.Resources;
using VetPax.Application.Modules.NutritionManagement.Models;
using VetPax.Application.Modules.NutritionManagement.UseCases;

namespace VetPax.Api.Modules.NutritionManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/pets/{petId:guid}/feeding-plans")]
[Produces("application/json")]
public sealed class PetFeedingPlansController : ControllerBase
{
    private readonly CreateFeedingPlanHandler _create;
    private readonly GetActiveFeedingPlanHandler _getActive;

    public PetFeedingPlansController(
        CreateFeedingPlanHandler create,
        GetActiveFeedingPlanHandler getActive)
    {
        _create = create;
        _getActive = getActive;
    }

    /// <summary>
    /// US08 / TS07 - Prescribe a feeding plan for a pet.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(FeedingPlanResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] Guid petId,
        [FromBody] CreateFeedingPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = FeedingPlanRequestMapper.ToCommand(
                petId,
                request);

            var result = await _create.HandleAsync(
                command,
                cancellationToken);

            var response = FeedingPlanResponse.From(result);

            return CreatedAtRoute(
                FeedingPlansController.GetByIdRouteName,
                new { planId = response.PlanId },
                response);
        }
        catch (Exception ex)
            when (NutritionHttpErrors.IsExpected(ex))
        {
            return NutritionHttpErrors.ToActionResult(this, ex);
        }
    }

    /// <summary>
    /// US24 - Get the active feeding plan of a pet.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(
        typeof(FeedingPlanResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveAsync(
        [FromRoute] Guid petId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getActive.HandleAsync(
                new GetActiveFeedingPlanQuery(petId),
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
