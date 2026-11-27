using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VetPax.Api.Realtime;

public sealed class RealtimeWebSocketManager
{
    private sealed record Connection(WebSocket Socket, SemaphoreSlim SendLock);

    private readonly ConcurrentDictionary<Guid, Connection> _connections = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task RunAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var connectionId = Guid.NewGuid();
        var connection = new Connection(socket, new SemaphoreSlim(1, 1));
        _connections[connectionId] = connection;

        try
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed.",
                        CancellationToken.None);
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The HTTP request ended or the application is stopping.
        }
        catch (WebSocketException)
        {
            // A disconnected client is removed in the finally block.
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
        }
    }

    public async Task PublishAsync(
        RealtimeEvent message,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
        var segment = new ArraySegment<byte>(payload);

        foreach (var pair in _connections.ToArray())
        {
            var connection = pair.Value;
            if (connection.Socket.State != WebSocketState.Open)
            {
                _connections.TryRemove(pair.Key, out _);
                continue;
            }

            await connection.SendLock.WaitAsync(cancellationToken);
            try
            {
                await connection.Socket.SendAsync(
                    segment,
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken);
            }
            catch (WebSocketException)
            {
                _connections.TryRemove(pair.Key, out _);
            }
            catch (ObjectDisposedException)
            {
                _connections.TryRemove(pair.Key, out _);
            }
            catch (InvalidOperationException)
            {
                _connections.TryRemove(pair.Key, out _);
            }
            finally
            {
                connection.SendLock.Release();
            }
        }
    }
}
