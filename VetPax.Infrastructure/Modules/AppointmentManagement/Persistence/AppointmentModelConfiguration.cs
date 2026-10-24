
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.AppointmentManagement.Enums;

namespace VetPax.Infrastructure.Modules.AppointmentManagement.Persistence;

public static class AppointmentModelConfiguration
{
    public static void ConfigureAppointmentManagement(
        this ModelBuilder modelBuilder)
    {
        ConfigureAppointments(modelBuilder);
        ConfigureOutbox(modelBuilder);
        ConfigureReminders(modelBuilder);
        ConfigureClinicalAttentionLinks(modelBuilder);
    }

    private static void ConfigureAppointments(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentRow>(entity =>
        {
            entity.ToTable("appointments");

            entity.HasKey(x => x.AppointmentId);

            entity.Property(x => x.AppointmentId)
                .HasColumnName("appointment_id")
                .ValueGeneratedNever();

            entity.Property(x => x.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            entity.Property(x => x.PetId)
                .HasColumnName("pet_id")
                .IsRequired();

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id")
                .IsRequired();

            entity.Property(x => x.VeterinarianId)
                .HasColumnName("veterinarian_id")
                .IsRequired();

            entity.Property(x => x.AppointmentDate)
                .HasColumnName("appointment_date")
                .HasColumnType("date")
                .IsRequired();

            entity.Property(x => x.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("time(6)")
                .IsRequired();

            entity.Property(x => x.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("time(6)")
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasColumnName("reason")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(x => x.ActiveSlotTime)
                .HasColumnName("active_slot_time")
                .HasColumnType("time(6)")
                .HasComputedColumnSql(
                    "CASE WHEN `status` IN ('Pending', 'Attended') " +
                    "THEN `start_time` ELSE NULL END",
                    stored: true);

            entity.Property(x => x.ClinicalAttentionId)
                .HasColumnName("clinical_attention_id");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.Property(x => x.Version)
                .HasColumnName("version")
                .IsConcurrencyToken()
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.VeterinarianId,
                x.AppointmentDate,
                x.ActiveSlotTime
            })
            .IsUnique()
            .HasDatabaseName("ux_appointments_vet_active_slot");

            entity.HasIndex(x => new
            {
                x.ClinicId,
                x.AppointmentDate
            })
            .HasDatabaseName("ix_appointments_clinic_date");

            entity.HasIndex(x => new
            {
                x.VeterinarianId,
                x.AppointmentDate,
                x.StartTime
            })
            .HasDatabaseName("ix_appointments_vet_agenda");

            entity.HasIndex(x => x.OwnerId)
                .HasDatabaseName("ix_appointments_owner");

            // IDs de otros bounded contexts:
            // referencias lógicas, sin claves foráneas.
        });
    }

    private static void ConfigureOutbox(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentOutboxRow>(entity =>
        {
            entity.ToTable("appointment_outbox");

            entity.HasKey(x => x.EventId);

            entity.Property(x => x.EventId)
                .HasColumnName("event_id")
                .ValueGeneratedNever();

            entity.Property(x => x.AppointmentId)
                .HasColumnName("appointment_id")
                .IsRequired();

            entity.Property(x => x.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Payload)
                .HasColumnName("payload")
                .HasColumnType("longtext")
                .IsRequired();

            entity.Property(x => x.OccurredAtUtc)
                .HasColumnName("occurred_at_utc")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.Property(x => x.NextAttemptAtUtc)
                .HasColumnName("next_attempt_at_utc")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.Property(x => x.PublishedAtUtc)
                .HasColumnName("published_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.Attempts)
                .HasColumnName("attempts")
                .HasDefaultValue(0);

            entity.Property(x => x.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(1024);

            entity.HasIndex(x => new
            {
                x.PublishedAtUtc,
                x.NextAttemptAtUtc
            })
            .HasDatabaseName("ix_appointment_outbox_pending");
        });
    }

    private static void ConfigureReminders(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentReminderRow>(entity =>
        {
            entity.ToTable("appointment_reminders");

            entity.HasKey(x => x.AppointmentId);

            entity.Property(x => x.AppointmentId)
                .HasColumnName("appointment_id")
                .ValueGeneratedNever();

            entity.Property(x => x.OwnerId)
                .HasColumnName("owner_id")
                .IsRequired();

            entity.Property(x => x.DueAtUtc)
                .HasColumnName("due_at_utc")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(x => x.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.Property(x => x.SentAtUtc)
                .HasColumnName("sent_at_utc")
                .HasColumnType("datetime(6)");

            entity.HasIndex(x => new
            {
                x.Status,
                x.DueAtUtc
            })
            .HasDatabaseName("ix_appointment_reminders_due");

            entity.HasOne<AppointmentRow>()
                .WithOne()
                .HasForeignKey<AppointmentReminderRow>(
                    x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureClinicalAttentionLinks(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentAttentionLinkRow>(entity =>
        {
            entity.ToTable("appointment_attention_links");

            entity.HasKey(x => x.AppointmentId);

            entity.Property(x => x.AppointmentId)
                .HasColumnName("appointment_id")
                .ValueGeneratedNever();

            entity.Property(x => x.ClinicalAttentionId)
                .HasColumnName("clinical_attention_id")
                .IsRequired();

            entity.Property(x => x.LinkedAtUtc)
                .HasColumnName("linked_at_utc")
                .HasColumnType("datetime(6)")
                .IsRequired();

            entity.HasIndex(x => x.ClinicalAttentionId)
                .IsUnique()
                .HasDatabaseName("ux_appointment_attention_links_attention");

            entity.HasOne<AppointmentRow>()
                .WithOne()
                .HasForeignKey<AppointmentAttentionLinkRow>(
                    x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
