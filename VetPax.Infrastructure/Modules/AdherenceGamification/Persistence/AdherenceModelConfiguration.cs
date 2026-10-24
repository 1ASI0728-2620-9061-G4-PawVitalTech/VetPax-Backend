
using Microsoft.EntityFrameworkCore;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Persistence;

public static class AdherenceModelConfiguration
{
    public static ModelBuilder ApplyAdherenceGamificationModel(
        this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdherenceRow>(entity =>
        {
            entity.ToTable("adherence_profiles");

            entity.HasKey(x => x.OwnerId);

            entity.Property(x => x.OwnerId)
                .HasColumnName("owner_id")
                .ValueGeneratedNever();

            entity.Property(x => x.EvidenceCount)
                .HasColumnName("evidence_count");

            entity.Property(x => x.Score)
                .HasColumnName("score")
                .HasPrecision(18, 4);

            entity.Property(x => x.Level)
                .HasColumnName("level")
                .HasMaxLength(20);

            entity.Property(x => x.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.ConcurrencyStamp)
                .HasColumnName("concurrency_stamp")
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<ProcessedAdherenceEventRow>(entity =>
        {
            entity.ToTable("adherence_processed_events");

            entity.HasKey(x => x.EventId);

            entity.Property(x => x.EventId)
                .HasColumnName("event_id")
                .ValueGeneratedNever();

            entity.Property(x => x.OwnerId)
                .HasColumnName("owner_id");

            entity.Property(x => x.Kind)
                .HasColumnName("kind")
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(x => x.SourceId)
                .HasColumnName("source_id");

            entity.Property(x => x.OccurredAtUtc)
                .HasColumnName("occurred_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.ProcessedAtUtc)
                .HasColumnName("processed_at_utc")
                .HasColumnType("datetime(6)");

            entity.HasIndex(x => new
            {
                x.Kind,
                x.SourceId
            }).IsUnique();

            entity.HasIndex(x => x.OwnerId);
        });

        modelBuilder.Entity<AdherenceRecognitionRow>(entity =>
        {
            entity.ToTable("adherence_recognitions");

            entity.HasKey(x => x.RecognitionId);

            entity.Property(x => x.RecognitionId)
                .HasColumnName("recognition_id")
                .ValueGeneratedNever();

            entity.Property(x => x.OwnerId)
                .HasColumnName("owner_id");

            entity.Property(x => x.FromLevel)
                .HasColumnName("from_level")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.ToLevel)
                .HasColumnName("to_level")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.SentAtUtc)
                .HasColumnName("sent_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.DeliveryStatus)
                .HasColumnName("delivery_status")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.AttemptCount)
                .HasColumnName("attempt_count");

            entity.Property(x => x.NextAttemptAtUtc)
                .HasColumnName("next_attempt_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.LeaseId)
                .HasColumnName("lease_id");

            entity.Property(x => x.LockedUntilUtc)
                .HasColumnName("locked_until_utc")
                .HasColumnType("datetime(6)");

            entity.HasIndex(x => new
            {
                x.DeliveryStatus,
                x.NextAttemptAtUtc,
                x.LockedUntilUtc
            });

            entity.HasOne<AdherenceRow>()
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdherenceOutboxRow>(entity =>
        {
            entity.ToTable("adherence_outbox");

            entity.HasKey(x => x.EventId);

            entity.Property(x => x.EventId)
                .HasColumnName("event_id")
                .ValueGeneratedNever();

            entity.Property(x => x.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.PayloadJson)
                .HasColumnName("payload_json")
                .HasColumnType("longtext")
                .IsRequired();

            entity.Property(x => x.OccurredAtUtc)
                .HasColumnName("occurred_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.PublishedAtUtc)
                .HasColumnName("published_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.AttemptCount)
                .HasColumnName("attempt_count");

            entity.HasIndex(x => x.PublishedAtUtc);
        });

        return modelBuilder;
    }
}
