using System.Net.WebSockets;

using VetPax.Application.Modules.IdentityAccessManagement.Ports;
using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;

namespace VetPax.Api.Realtime;

public static class RealtimeWebSocketEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        RealtimeWebSocketManager connections)
    {
        // Verify authentication.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return;
        }

        // Get the authenticated identity from Keycloak.
        var currentUser = context.RequestServices
            .GetRequiredService<IAuthenticatedUserPort>();

        var identity = await currentUser.GetCurrentAsync(
            context.RequestAborted);

        if (identity is null ||
            identity.UserId.Value == Guid.Empty)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return;
        }

        // Only owners and veterinarians can receive updates.
        if (identity.Role != UserRole.Owner &&
            identity.Role != UserRole.Veterinarian)
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return;
        }

        // Read access token expiration.
        var expirationClaim =
            context.User.FindFirst("exp")?.Value;

        if (!long.TryParse(
                expirationClaim,
                out var expiration))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return;
        }

        var expiresAt =
            DateTimeOffset.FromUnixTimeSeconds(expiration);

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return;
        }

        // Verify the WebSocket handshake.
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            return;
        }

        using var socket =
            await context.WebSockets.AcceptWebSocketAsync();

        await connections.RunAsync(
            socket,
            identity.UserId.Value,
            identity.Role,
            expiresAt,
            context.RequestAborted);
    }
}