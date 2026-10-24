using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;

public sealed class MedicationDeviceTokenConfiguration
    : IEntityTypeConfiguration<MedicationDeviceTokenRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationDeviceTokenRow> entity)
    {
        entity.ToTable("medication_device_tokens");

        entity.HasKey(x => x.DeviceId);

        entity.Property(x => x.DeviceId)
            .HasColumnName("device_id")
            .ValueGeneratedNever();

        entity.Property(x => x.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        entity.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        entity.Property(x => x.Token)
            .HasColumnName("fcm_token")
            .HasMaxLength(4096)
            .IsRequired();

        entity.Property(x => x.Platform)
            .HasColumnName("platform")
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        entity.Property(x => x.LastSeenAtUtc)
            .HasColumnName("last_seen_at_utc")
            .HasColumnType("datetime(6)")
            .IsRequired();

        entity.HasIndex(x => x.TokenHash)
            .IsUnique();

        entity.HasIndex(x => new
        {
            x.OwnerId,
            x.IsActive
        });
    }
}