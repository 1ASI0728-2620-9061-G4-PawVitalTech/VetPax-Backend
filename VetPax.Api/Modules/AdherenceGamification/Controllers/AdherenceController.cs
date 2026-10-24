using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.AdherenceGamification.Resources;
using VetPax.Application.Modules.AdherenceGamification.Handlers;
using VetPax.Application.Modules.AdherenceGamification.Queries;

namespace VetPax.Api.Modules.AdherenceGamification.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me/adherence")]
[Produces("application/json")]
public sealed class AdherenceController : ControllerBase
{
    private readonly GetMyAdherenceHandler _handler;

    public AdherenceController(GetMyAdherenceHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// US13: Get the current owner's constancy progress.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(AdherenceResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _handler.HandleAsync(
                new GetMyAdherenceQuery(),
                cancellationToken);

            return Ok(AdherenceResponse.From(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}