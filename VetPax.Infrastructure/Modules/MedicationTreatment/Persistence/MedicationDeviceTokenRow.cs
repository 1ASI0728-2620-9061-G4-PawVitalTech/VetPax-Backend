namespace VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;

public sealed class MedicationDeviceTokenRow
{
    public Guid DeviceId { get; set; }
    public Guid OwnerId { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}