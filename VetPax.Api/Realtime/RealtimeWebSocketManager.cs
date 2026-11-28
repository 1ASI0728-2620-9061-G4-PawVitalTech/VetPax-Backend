using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

using VetPax.Application.Modules.AppointmentManagement.Models;
using VetPax.Application.Modules.AppointmentManagement.Ports;
using VetPax.Application.Modules.PetClinicalCare.Ports;

using VetPax.Domain.Modules.IdentityAccessManagement.ValueObjects;
using VetPax.Domain.Modules.PetClinicalCare.Repositories;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Domain.Modules.AppointmentManagement.Repositories;
using VetPax.Domain.Modules.AppointmentManagement.ValueObjects;

namespace VetPax.Api.Realtime;

public sealed class RealtimeWebSocketManager
{
    private sealed record Connection(
        Guid UserId,
        UserRole Role,
        DateTimeOffset ExpiresAt,
        WebSocket Socket,
        SemaphoreSlim SendLock);

    private readonly ConcurrentDictionary<Guid, Connection>
        _connections = new();

    private readonly JsonSerializerOptions _jsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<RealtimeWebSocketManager> _logger;

    public RealtimeWebSocketManager(
        IServiceScopeFactory scopeFactory,
        ILogger<RealtimeWebSocketManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // -------------------------------------------------
    // Register and maintain a WebSocket connection
    // -------------------------------------------------

    public async Task RunAsync(
        WebSocket socket,
        Guid userId,
        UserRole role,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var remaining = expiresAt - DateTimeOffset.UtcNow;

        if (remaining <= TimeSpan.Zero)
        {
            socket.Abort();
            return;
        }

        using var lifetime =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        lifetime.CancelAfter(remaining);

        var connectionId = Guid.NewGuid();

        var connection = new Connection(
            userId,
            role,
            expiresAt,
            socket,
            new SemaphoreSlim(1, 1));

        _connections[connectionId] = connection;

        try
        {
            var buffer = new byte[1024];

            while (socket.State == WebSocketState.Open &&
                   !lifetime.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    lifetime.Token);

                if (result.MessageType ==
                    WebSocketMessageType.Close)
                {
                    using var closeTimeout =
                        new CancellationTokenSource(
                            TimeSpan.FromSeconds(2));

                    await connection.SendLock.WaitAsync(
                        closeTimeout.Token);

                    try
                    {
                        if (socket.State ==
                            WebSocketState.CloseReceived)
                        {
                            await socket.CloseOutputAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Connection closed.",
                                closeTimeout.Token);
                        }
                    }
                    finally
                    {
                        connection.SendLock.Release();
                    }

                    break;
                }

                // Business operations are performed through REST.
                // WebSocket is used only to receive updates.
            }
        }
        catch (OperationCanceledException)
        {
            // Connection ended or access token expired.
        }
        catch (WebSocketException)
        {
            // Client disconnected.
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);

            if (socket.State != WebSocketState.Closed)
                socket.Abort();
        }
    }

    // -------------------------------------------------
    // Appointment updates
    // -------------------------------------------------

    public Task PublishAppointmentAsync(
        AppointmentData appointment)
    {
        return PublishAuthorizedAsync(
            RealtimeEvent.Create("appointment.updated"),

            async (connection, services, ct) =>
            {
                // Only the owner of the appointment.
                if (connection.Role == UserRole.Owner)
                {
                    return connection.UserId ==
                           appointment.OwnerId;
                }

                // Only the assigned veterinarian.
                if (connection.Role != UserRole.Veterinarian ||
                    connection.UserId !=
                    appointment.VeterinarianId)
                {
                    return false;
                }

                var clinics = services.GetRequiredService<
                    IAppointmentClinicPort>();

                // Verify that the membership is still active.
                return await clinics.IsActiveVeterinarianAsync(
                    connection.UserId,
                    appointment.ClinicId,
                    ct);
            });
    }
    
    public async Task PublishAppointmentAsync(Guid appointmentId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(2));

            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var repository = scope.ServiceProvider
                .GetRequiredService<IAppointmentRepository>();

            var appointment = await repository.FindByIdAsync(
                new AppointmentId(appointmentId),
                timeout.Token);

            if (appointment is null)
                return;

            await PublishAppointmentAsync(
                AppointmentData.From(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not publish realtime update for appointment {AppointmentId}",
                appointmentId);
        }
    }

    // -------------------------------------------------
    // Clinical record updates
    // -------------------------------------------------

    public Task PublishClinicalAsync(
        Guid petId,
        Guid clinicId)
    {
        return PublishAuthorizedAsync(
            RealtimeEvent.Create("clinical.attention.created"),

            async (connection, services, ct) =>
            {
                if (connection.Role == UserRole.Owner)
                {
                    var pets = services.GetRequiredService<
                        IPetRepository>();

                    var pet = await pets.FindByIdAsync(
                        new PetId(petId),
                        ct);

                    return pet is not null &&
                           pet.OwnerId.Value == connection.UserId;
                }

                if (connection.Role != UserRole.Veterinarian)
                    return false;

                var authorization =
                    services.GetRequiredService<
                        IClinicalAuthorizationPort>();

                return await authorization.CanViewEvolutionAsync(
                    connection.UserId,
                    new ClinicId(clinicId),
                    new PetId(petId),
                    ct);
            });
    }

    // -------------------------------------------------
    // Authorized event distribution
    // -------------------------------------------------

    private async Task PublishAuthorizedAsync(
        RealtimeEvent message,
        Func<Connection, IServiceProvider,
            CancellationToken, Task<bool>> canReceive)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            message,
            _jsonOptions);

        var tasks = _connections.ToArray()
            .Select(pair => SendIfAuthorizedAsync(
                pair.Key,
                pair.Value,
                payload,
                canReceive));

        await Task.WhenAll(tasks);
    }

    private async Task SendIfAuthorizedAsync(
        Guid connectionId,
        Connection connection,
        byte[] payload,
        Func<Connection, IServiceProvider,
            CancellationToken, Task<bool>> canReceive)
    {
        if (connection.Socket.State != WebSocketState.Open ||
            connection.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _connections.TryRemove(connectionId, out _);
            connection.Socket.Abort();
            return;
        }

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(2));

        var ct = timeout.Token;

        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            await connection.SendLock.WaitAsync(ct);

            try
            {
                var authorized = await canReceive(
                    connection,
                    scope.ServiceProvider,
                    ct);

                if (!authorized)
                    return;

                if (connection.Socket.State !=
                    WebSocketState.Open ||
                    connection.ExpiresAt <=
                    DateTimeOffset.UtcNow)
                {
                    return;
                }

                await connection.Socket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: ct);
            }
            finally
            {
                connection.SendLock.Release();
            }
        }
        catch (Exception ex)
        {
            _connections.TryRemove(connectionId, out _);

            connection.Socket.Abort();

            _logger.LogWarning(
                ex,
                "Realtime event delivery failed.");
        }
    }
}