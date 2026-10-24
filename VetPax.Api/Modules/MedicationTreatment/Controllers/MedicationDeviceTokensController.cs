
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

namespace VetPax.Api.Modules.MedicationTreatment.Controllers;

public sealed record RegisterMedicationDeviceRequest(
    [property: JsonRequired] string Token,
    [property: JsonRequired] string Platform);

public sealed record MedicationDeviceResponse(Guid DeviceId);

[ApiController]
[Authorize(Roles = "owner")]
[Route("api/v1/me/medication-devices")]
public sealed class MedicationDeviceTokensController : ControllerBase
{
    private readonly MedicationDeviceTokenStore _tokens;

    public MedicationDeviceTokensController(
        MedicationDeviceTokenStore tokens)
    {
        _tokens = tokens;
    }

    // POST /api/v1/me/medication-devices
    [HttpPost]
    [ProducesResponseType(
        typeof(MedicationDeviceResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterMedicationDeviceRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
            return Forbid();

        if (request is null ||
            string.IsNullOrWhiteSpace(request.Token) ||
            request.Token.Length > 4096 ||
            string.IsNullOrWhiteSpace(request.Platform) ||
            request.Platform.Trim().ToLowerInvariant()
                is not ("android" or "ios" or "web"))
        {
            return BadRequest(new
            {
                message = "A valid token and platform are required."
            });
        }

        var deviceId = await _tokens.RegisterAsync(
            ownerId,
            request.Token,
            request.Platform,
            cancellationToken);

        return Ok(new MedicationDeviceResponse(deviceId));
    }

    // DELETE /api/v1/me/medication-devices/{deviceId}
    [HttpDelete("{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unregister(
        [FromRoute] Guid deviceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
            return Forbid();

        var disabled = await _tokens.DisableAsync(
            ownerId,
            deviceId,
            cancellationToken);

        return disabled ? NoContent() : NotFound();
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        return Guid.TryParse(
                   User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                   out ownerId)
               || Guid.TryParse(
                   User.FindFirst("sub")?.Value,
                   out ownerId);
    }
}
