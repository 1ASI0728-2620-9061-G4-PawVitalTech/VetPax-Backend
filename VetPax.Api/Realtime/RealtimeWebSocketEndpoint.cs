using System.Net.WebSockets;

namespace VetPax.Api.Realtime;

public static class RealtimeWebSocketEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        RealtimeWebSocketManager connections)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        await connections.RunAsync(socket, context.RequestAborted);
    }
}
