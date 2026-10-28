
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Notifications;

public sealed class MedicationDeviceTokenStore
{
    private readonly VetPaxDbContext _db;
    private readonly TimeProvider _clock;

    public MedicationDeviceTokenStore(
        VetPaxDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public async Task<Guid> RegisterAsync(
        Guid ownerId,
        string token,
        string platform,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
            throw new ArgumentException("Invalid owner ID.");

        if (string.IsNullOrWhiteSpace(token) ||
            token.Length > 4096)
        {
            throw new ArgumentException("Invalid FCM token.");
        }

        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Platform is required.");

        platform = platform.Trim().ToLowerInvariant();

        if (platform is not ("android" or "ios" or "web"))
            throw new ArgumentException("Unsupported platform.");

        token = token.Trim();

        var hash = HashToken(token);

        var existing = await _db
            .Set<MedicationDeviceTokenRow>()
            .SingleOrDefaultAsync(
                x => x.TokenHash == hash,
                cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;

        if (existing is null)
        {
            existing = new MedicationDeviceTokenRow
            {
                DeviceId = Guid.NewGuid(),
                OwnerId = ownerId,
                TokenHash = hash,
                Token = token,
                Platform = platform,
                IsActive = true,
                LastSeenAtUtc = now
            };

            await _db.Set<MedicationDeviceTokenRow>()
                .AddAsync(existing, cancellationToken);
        }
        else
        {
            // El dispositivo puede cambiar de cuenta.
            // Nunca debe seguir recibiendo recordatorios
            // del propietario anterior.
            existing.OwnerId = ownerId;
            existing.Token = token;
            existing.Platform = platform;
            existing.IsActive = true;
            existing.LastSeenAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return existing.DeviceId;
    }

    public async Task<IReadOnlyList<MedicationDeviceTokenRow>>
        GetActiveTokensAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default)
    {
        var freshnessLimit = _clock.GetUtcNow()
            .AddDays(-30)
            .UtcDateTime;

        return await _db.Set<MedicationDeviceTokenRow>()
            .AsNoTracking()
            .Where(x =>
                x.OwnerId == ownerId &&
                x.IsActive &&
                x.LastSeenAtUtc >= freshnessLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DisableAsync(
        Guid ownerId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var device = await _db
            .Set<MedicationDeviceTokenRow>()
            .SingleOrDefaultAsync(
                x => x.OwnerId == ownerId &&
                     x.DeviceId == deviceId,
                cancellationToken);

        if (device is null)
            return false;

        device.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
