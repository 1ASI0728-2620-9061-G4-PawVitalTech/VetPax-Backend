
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetPax.Api.Modules.IdentityAccessManagement.Resources;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.AuthenticateUser;
using VetPax.Application.Modules.IdentityAccessManagement.UseCases.ValidatePermission;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Api.Modules.IdentityAccessManagement.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/auth/me/permissions")]
[Produces("application/json")]
public sealed class PermissionsController : ControllerBase
{
    private readonly AuthenticateUserHandler _authenticate;
    private readonly ValidatePermissionHandler _validatePermission;

    public PermissionsController(
        AuthenticateUserHandler authenticate,
        ValidatePermissionHandler validatePermission)
    {
        _authenticate = authenticate;
        _validatePermission = validatePermission;
    }

    /// <summary>
    /// Returns permissions granted to the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(MyPermissionsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyPermissionsAsync(
        CancellationToken cancellationToken)
    {
        var current = await _authenticate.HandleAsync(
            cancellationToken);

        if (current is null)
            return Forbid();

        var allowedPermissions = new List<string>();

        foreach (var permission in Enum.GetValues<AccessPermission>())
        {
            var allowed = await _validatePermission.HandleAsync(
                permission,
                cancellationToken);

            if (allowed)
                allowedPermissions.Add(
                    permission.ToString());
        }

        return Ok(new MyPermissionsResponse(
            allowedPermissions));
    }
}
