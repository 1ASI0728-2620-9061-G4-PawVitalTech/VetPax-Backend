
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VetPax.Infrastructure.Modules.MedicationTreatment.Persistence;

public static class MedicationModelBuilderExtensions
{
    public static ModelBuilder ApplyMedicationTreatmentModel(
        this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new MedicationPlanConfiguration());

        modelBuilder.ApplyConfiguration(
            new MedicationScheduleConfiguration());

        modelBuilder.ApplyConfiguration(
            new MedicationDoseConfiguration());

        modelBuilder.ApplyConfiguration(
            new MedicationAdministrationConfiguration());

        modelBuilder.ApplyConfiguration(
            new MedicationReminderPreferenceConfiguration());

        modelBuilder.ApplyConfiguration(
            new MedicationReminderConfiguration());

        modelBuilder.ApplyConfiguration(
            new MedicationOutboxConfiguration());
        
        modelBuilder.ApplyConfiguration(
            new MedicationDeviceTokenConfiguration());


        return modelBuilder;
    }
}

public sealed class MedicationPlanConfiguration
    : IEntityTypeConfiguration<MedicationPlanRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationPlanRow> b)
    {
        b.ToTable("medication_plans");
        b.HasKey(x => x.PlanId);

        b.Property(x => x.PlanId)
            .HasColumnName("plan_id")
            .ValueGeneratedNever();

        b.Property(x => x.OwnerId).HasColumnName("owner_id");
        b.Property(x => x.PetId).HasColumnName("pet_id");
        b.Property(x => x.ClinicId).HasColumnName("clinic_id");
        b.Property(x => x.VeterinarianId)
            .HasColumnName("veterinarian_id");
        b.Property(x => x.ClinicalAttentionId)
            .HasColumnName("clinical_attention_id");

        b.Property(x => x.MedicationName)
            .HasColumnName("medication_name")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Route)
            .HasColumnName("route")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.Instructions)
            .HasColumnName("instructions")
            .HasMaxLength(2000)
            .IsRequired();

        b.Property(x => x.TimeZoneId)
            .HasColumnName("time_zone_id")
            .HasMaxLength(100)
            .IsRequired();

        b.Property(x => x.StartDate)
            .HasColumnName("start_date");

        b.Property(x => x.EndDate)
            .HasColumnName("end_date");

        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        b.Property(x => x.RemindersEnabled)
            .HasColumnName("reminders_enabled");

        b.Property(x => x.ScheduleVersion)
            .HasColumnName("schedule_version");

        b.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        b.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasPrecision(6);

        b.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasPrecision(6);

        b.HasIndex(x => x.PetId);
        b.HasIndex(x => new { x.ClinicId, x.VeterinarianId });
        b.HasIndex(x => new { x.Status, x.EndDate });
    }
}

public sealed class MedicationScheduleConfiguration
    : IEntityTypeConfiguration<MedicationScheduleRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationScheduleRow> b)
    {
        b.ToTable("medication_schedules");
        b.HasKey(x => x.ScheduleId);

        b.Property(x => x.ScheduleId)
            .HasColumnName("schedule_id")
            .ValueGeneratedNever();

        b.Property(x => x.PlanId).HasColumnName("plan_id");
        b.Property(x => x.ScheduleVersion)
            .HasColumnName("schedule_version");

        b.Property(x => x.LocalTime)
            .HasColumnName("local_time");

        b.Property(x => x.DaysMask)
            .HasColumnName("days_mask");

        b.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 4);

        b.Property(x => x.Unit)
            .HasColumnName("unit")
            .HasMaxLength(30)
            .IsRequired();

        b.HasOne<MedicationPlanRow>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new
        {
            x.PlanId,
            x.ScheduleVersion
        });
    }
}

public sealed class MedicationDoseConfiguration
    : IEntityTypeConfiguration<MedicationDoseRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationDoseRow> b)
    {
        b.ToTable("medication_doses");
        b.HasKey(x => x.DoseId);

        b.Property(x => x.DoseId)
            .HasColumnName("dose_id")
            .ValueGeneratedNever();

        b.Property(x => x.PlanId).HasColumnName("plan_id");
        b.Property(x => x.ScheduleId).HasColumnName("schedule_id");
        b.Property(x => x.ScheduleVersion)
            .HasColumnName("schedule_version");
        b.Property(x => x.LocalDate).HasColumnName("local_date");
        b.Property(x => x.LocalTime).HasColumnName("local_time");

        b.Property(x => x.ScheduledAtUtc)
            .HasColumnName("scheduled_at_utc")
            .HasPrecision(6);

        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        b.HasOne<MedicationPlanRow>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<MedicationScheduleRow>()
            .WithMany()
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new
        {
            x.PlanId,
            x.ScheduleVersion,
            x.ScheduleId,
            x.LocalDate
        }).IsUnique();

        b.HasIndex(x => new
        {
            x.Status,
            x.ScheduledAtUtc
        });
    }
}

public sealed class MedicationAdministrationConfiguration
    : IEntityTypeConfiguration<MedicationAdministrationRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationAdministrationRow> b)
    {
        b.ToTable("medication_administrations");
        b.HasKey(x => x.AdministrationId);

        b.Property(x => x.AdministrationId)
            .HasColumnName("administration_id")
            .ValueGeneratedNever();

        b.Property(x => x.DoseId).HasColumnName("dose_id");
        b.Property(x => x.AdministeredBy)
            .HasColumnName("administered_by");

        b.Property(x => x.AdministeredAtUtc)
            .HasColumnName("administered_at_utc")
            .HasPrecision(6);

        b.Property(x => x.RecordedAtUtc)
            .HasColumnName("recorded_at_utc")
            .HasPrecision(6);

        b.HasOne<MedicationDoseRow>()
            .WithMany()
            .HasForeignKey(x => x.DoseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DoseId).IsUnique();
    }
}

public sealed class MedicationReminderPreferenceConfiguration
    : IEntityTypeConfiguration<MedicationReminderPreferenceRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationReminderPreferenceRow> b)
    {
        b.ToTable("medication_reminder_preferences");
        b.HasKey(x => x.PlanId);

        b.Property(x => x.PlanId).HasColumnName("plan_id");
        b.Property(x => x.Enabled).HasColumnName("enabled");

        b.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasPrecision(6);

        b.HasOne<MedicationPlanRow>()
            .WithOne()
            .HasForeignKey<MedicationReminderPreferenceRow>(
                x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MedicationReminderConfiguration
    : IEntityTypeConfiguration<MedicationReminderRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationReminderRow> b)
    {
        b.ToTable("medication_reminders");
        b.HasKey(x => x.ReminderId);

        b.Property(x => x.ReminderId)
            .HasColumnName("reminder_id")
            .ValueGeneratedNever();

        b.Property(x => x.PlanId).HasColumnName("plan_id");
        b.Property(x => x.DoseId).HasColumnName("dose_id");
        b.Property(x => x.OwnerId).HasColumnName("owner_id");

        b.Property(x => x.DueAtUtc)
            .HasColumnName("due_at_utc")
            .HasPrecision(6);

        b.Property(x => x.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasPrecision(6);

        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        b.Property(x => x.Attempts)
            .HasColumnName("attempts");

        b.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(1024);

        b.Property(x => x.SentAtUtc)
            .HasColumnName("sent_at_utc")
            .HasPrecision(6);

        b.Property(x => x.LockedUntilUtc)
            .HasColumnName("locked_until_utc")
            .HasPrecision(6);

        b.Property(x => x.LeaseToken)
            .HasColumnName("lease_token");

        b.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasPrecision(6);

        b.HasOne<MedicationPlanRow>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<MedicationDoseRow>()
            .WithMany()
            .HasForeignKey(x => x.DoseId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.DoseId).IsUnique();

        b.HasIndex(x => new
        {
            x.Status,
            x.NextAttemptAtUtc
        });
    }
}

public sealed class MedicationOutboxConfiguration
    : IEntityTypeConfiguration<MedicationOutboxRow>
{
    public void Configure(
        EntityTypeBuilder<MedicationOutboxRow> b)
    {
        b.ToTable("medication_outbox");
        b.HasKey(x => x.EventId);

        b.Property(x => x.EventId)
            .HasColumnName("event_id")
            .ValueGeneratedNever();

        b.Property(x => x.PlanId)
            .HasColumnName("plan_id");

        b.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(200)
            .IsRequired();

        b.Property(x => x.Payload)
            .HasColumnName("payload")
            .HasColumnType("longtext")
            .IsRequired();

        b.Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasPrecision(6);

        b.Property(x => x.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .HasPrecision(6);

        b.Property(x => x.PublishedAtUtc)
            .HasColumnName("published_at_utc")
            .HasPrecision(6);

        b.Property(x => x.Attempts)
            .HasColumnName("attempts");

        b.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(1024);

        b.Property(x => x.LeaseToken)
            .HasColumnName("lease_token");

        b.Property(x => x.LockedUntilUtc)
            .HasColumnName("locked_until_utc")
            .HasPrecision(6);

        b.HasIndex(x => new
        {
            x.PublishedAtUtc,
            x.NextAttemptAtUtc
        });
    }
}
