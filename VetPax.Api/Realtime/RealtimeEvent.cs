namespace VetPax.Api.Realtime;

public sealed record RealtimeEvent(
    Guid EventId,
    string EventType,
    DateTime OccurredAtUtc)
{
    public static RealtimeEvent Create(string eventType) =>
        new(Guid.NewGuid(), eventType, DateTime.UtcNow);
}
