
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetPax.Application.Modules.NutritionManagement.Ports;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Notifications;

public sealed class NutritionFirebasePushGateway
    : INutritionPushGateway
{
    private readonly FirebaseMessaging _firebase;
    private readonly VetPaxDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ILogger<NutritionFirebasePushGateway> _logger;

    public NutritionFirebasePushGateway(
        FirebaseMessaging firebase,
        VetPaxDbContext db,
        TimeProvider clock,
        ILogger<NutritionFirebasePushGateway> logger)
    {
        _firebase = firebase;
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task SendAsync(
        Guid ownerId,
        Guid planId,
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty ||
            planId == Guid.Empty ||
            reminderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Valid owner, plan and reminder IDs are required.");
        }

        var lastValidDate =
            _clock.GetUtcNow().UtcDateTime.AddDays(-30);

        var devices = await _db
            .Set<MedicationDeviceTokenRow>()
            .AsNoTracking()
            .Where(device =>
                device.OwnerId == ownerId &&
                device.IsActive &&
                device.LastSeenAtUtc >= lastValidDate)
            .OrderByDescending(device => device.LastSeenAtUtc)
            .Take(50)
            .Select(device => new
            {
                device.Token,
                device.TokenHash
            })
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
        {
            throw new InvalidOperationException(
                "No active FCM devices registered for this owner.");
        }

        var successful = 0;

        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = new Message
            {
                Token = device.Token,

                Notification = new Notification
                {
                    Title = "Recordatorio de alimentación",
                    Body = "Es hora de revisar la alimentación de tu mascota."
                },

                Data = new Dictionary<string, string>
                {
                    ["type"] = "feeding_reminder",
                    ["planId"] = planId.ToString("D"),
                    ["reminderId"] = reminderId.ToString("D")
                }
            };

            try
            {
                await _firebase.SendAsync(
                    message,
                    cancellationToken);

                successful++;
            }
            catch (FirebaseMessagingException ex)
                when (ex.MessagingErrorCode ==
                      MessagingErrorCode.Unregistered)
            {
                // Invalidate only the exact owner's
                // token that Firebase rejected.
                await _db.Set<MedicationDeviceTokenRow>()
                    .Where(row =>
                        row.OwnerId == ownerId &&
                        row.TokenHash == device.TokenHash)
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            row => row.IsActive,
                            false),
                        cancellationToken);

                _logger.LogWarning(
                    "An expired FCM device token was disabled.");
            }
            catch (FirebaseMessagingException ex)
            {
                // Never log registration tokens.
                _logger.LogWarning(
                    "Nutrition FCM delivery failed. Code: {Code}",
                    ex.MessagingErrorCode);
            }
        }

        if (successful == 0)
        {
            throw new InvalidOperationException(
                "Firebase did not accept the feeding reminder.");
        }
    }
}
