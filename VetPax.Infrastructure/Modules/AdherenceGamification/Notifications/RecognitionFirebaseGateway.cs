
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Notifications;

public sealed class RecognitionFirebaseGateway : IRecognitionPushGateway
{
    private readonly VetPaxDbContext _db;
    private readonly FirebaseMessaging _firebase;
    private readonly TimeProvider _clock;
    private readonly ILogger<RecognitionFirebaseGateway> _logger;

    public RecognitionFirebaseGateway(
        VetPaxDbContext db,
        FirebaseMessaging firebase,
        TimeProvider clock,
        ILogger<RecognitionFirebaseGateway> logger)
    {
        _db = db;
        _firebase = firebase;
        _clock = clock;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid ownerId,
        Guid recognitionId,
        string newLevel,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty ||
            recognitionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid owner and recognition IDs are required.");
        }

        var lastValidDate =
            _clock.GetUtcNow().UtcDateTime.AddDays(-30);

        var devices = await _db.Set<MedicationDeviceTokenRow>()
            .AsNoTracking()
            .Where(x =>
                x.OwnerId == ownerId &&
                x.IsActive &&
                x.LastSeenAtUtc >= lastValidDate)
            .OrderByDescending(x => x.LastSeenAtUtc)
            .Take(50)
            .Select(x => new
            {
                x.Token,
                x.TokenHash
            })
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
            throw new InvalidOperationException(
                "No active FCM devices are registered.");

        var successful = 0;

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var message = new Message
                {
                    Token = device.Token,
                    Notification = new Notification
                    {
                        Title = "¡Nuevo nivel de constancia!",
                        Body = $"Alcanzaste el nivel " +
                            $"{TranslateLevel(newLevel)} en VetPax."
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["type"] = "adherence_level_promotion",
                        ["recognitionId"] = recognitionId.ToString("D"),
                        ["level"] = newLevel
                    }
                };

                await _firebase.SendAsync(
                    message,
                    cancellationToken);

                successful++;
            }
            catch (FirebaseMessagingException ex)
                when (ex.MessagingErrorCode ==
                      MessagingErrorCode.Unregistered)
            {
                await _db.Set<MedicationDeviceTokenRow>()
                    .Where(x =>
                        x.OwnerId == ownerId &&
                        x.TokenHash == device.TokenHash)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            x => x.IsActive,
                            false),
                        cancellationToken);
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogWarning(
                    "FCM rejected a recognition notification: {Code}",
                    ex.MessagingErrorCode);
            }
        }

        if (successful == 0)
            throw new InvalidOperationException(
                "Firebase did not accept the notification.");
    }

    private static string TranslateLevel(string level)
    {
        return level switch
        {
            "Bronze" => "Bronce",
            "Silver" => "Plata",
            "Gold" => "Oro",
            _ => level
        };
    }
}
