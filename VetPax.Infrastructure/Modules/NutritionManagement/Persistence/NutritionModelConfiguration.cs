
using Microsoft.EntityFrameworkCore;

namespace VetPax.Infrastructure.Modules.NutritionManagement.Persistence;

public static class NutritionModelBuilderExtensions
{
    public static ModelBuilder ApplyNutritionManagementModel(
        this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeedingPlanRow>(entity =>
        {
            entity.ToTable("nutrition_feeding_plans");

            entity.HasKey(x => x.PlanId);

            entity.Property(x => x.PlanId)
                .HasColumnName("plan_id")
                .ValueGeneratedNever();

            entity.Property(x => x.PetId)
                .HasColumnName("pet_id");

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id");

            entity.Property(x => x.VeterinarianId)
                .HasColumnName("veterinarian_id");

            entity.Property(x => x.FoodName)
                .HasColumnName("food_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Recommendations)
                .HasColumnName("recommendations")
                .HasMaxLength(4000);

            entity.Property(x => x.StartDate)
                .HasColumnName("start_date");

            entity.Property(x => x.EndDate)
                .HasColumnName("end_date");

            entity.Property(x => x.TimeZoneId)
                .HasColumnName("time_zone_id")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.RemindersEnabled)
                .HasColumnName("reminders_enabled");

            entity.Property(x => x.Version)
                .HasColumnName("version");

            entity.Property(x => x.ConcurrencyStamp)
                .HasColumnName("concurrency_stamp")
                .IsConcurrencyToken();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.UpdatedAtUtc)
                .HasColumnName("updated_at_utc")
                .HasColumnType("datetime(6)");

            entity.HasIndex(x => new
            {
                x.PetId,
                x.Status
            });

            entity.HasIndex(x => new
            {
                x.ClinicId,
                x.VeterinarianId
            });
        });

        modelBuilder.Entity<FeedingActivePlanRow>(entity =>
        {
            entity.ToTable("nutrition_active_plans");

            entity.HasKey(x => x.PetId);

            entity.Property(x => x.PetId)
                .HasColumnName("pet_id")
                .ValueGeneratedNever();

            entity.Property(x => x.PlanId)
                .HasColumnName("plan_id");

            entity.HasIndex(x => x.PlanId).IsUnique();

            entity.HasOne<FeedingPlanRow>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeedingScheduleRow>(entity =>
        {
            entity.ToTable("nutrition_feeding_schedules");

            entity.HasKey(x => new
            {
                x.PlanId,
                x.PlanVersion,
                x.ScheduleId
            });

            entity.Property(x => x.PlanId)
                .HasColumnName("plan_id");

            entity.Property(x => x.PlanVersion)
                .HasColumnName("plan_version");

            entity.Property(x => x.ScheduleId)
                .HasColumnName("schedule_id");

            entity.Property(x => x.LocalTime)
                .HasColumnName("local_time");

            entity.Property(x => x.DaysOfWeek)
                .HasColumnName("days_of_week")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(x => x.Quantity)
                .HasColumnName("quantity")
                .HasPrecision(18, 4);

            entity.Property(x => x.Unit)
                .HasColumnName("unit")
                .HasMaxLength(30)
                .IsRequired();

            entity.HasOne<FeedingPlanRow>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeedingPlanRevisionRow>(entity =>
        {
            entity.ToTable("nutrition_plan_revisions");

            entity.HasKey(x => new
            {
                x.PlanId,
                x.Version
            });

            entity.Property(x => x.PlanId)
                .HasColumnName("plan_id");

            entity.Property(x => x.Version)
                .HasColumnName("version");

            entity.Property(x => x.FoodName)
                .HasColumnName("food_name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Recommendations)
                .HasColumnName("recommendations")
                .HasMaxLength(4000);

            entity.Property(x => x.StartDate)
                .HasColumnName("start_date");

            entity.Property(x => x.EndDate)
                .HasColumnName("end_date");

            entity.Property(x => x.TimeZoneId)
                .HasColumnName("time_zone_id")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.ReplacedAtUtc)
                .HasColumnName("replaced_at_utc")
                .HasColumnType("datetime(6)");

            entity.HasOne<FeedingPlanRow>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeedingReminderRow>(entity =>
        {
            entity.ToTable("nutrition_reminders");

            entity.HasKey(x => x.ReminderId);

            entity.Property(x => x.ReminderId)
                .HasColumnName("reminder_id")
                .ValueGeneratedNever();

            entity.Property(x => x.PlanId)
                .HasColumnName("plan_id");

            entity.Property(x => x.PetId)
                .HasColumnName("pet_id");

            entity.Property(x => x.ScheduleId)
                .HasColumnName("schedule_id");

            entity.Property(x => x.PlanVersion)
                .HasColumnName("plan_version");

            entity.Property(x => x.LocalDate)
                .HasColumnName("local_date");

            entity.Property(x => x.ScheduledAtUtc)
                .HasColumnName("scheduled_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.ProcessedAtUtc)
                .HasColumnName("processed_at_utc")
                .HasColumnType("datetime(6)");

            entity.HasIndex(x => new
            {
                x.PlanId,
                x.PlanVersion,
                x.ScheduleId,
                x.LocalDate
            }).IsUnique();

            entity.HasIndex(x => new
            {
                x.Status,
                x.ScheduledAtUtc
            });

            entity.HasOne<FeedingPlanRow>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(x => x.AttemptCount)
                .HasColumnName("attempt_count")
                .HasDefaultValue(0);

            entity.Property(x => x.NextAttemptAtUtc)
                .HasColumnName("next_attempt_at_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.LeaseId)
                .HasColumnName("lease_id");

            entity.Property(x => x.LockedUntilUtc)
                .HasColumnName("locked_until_utc")
                .HasColumnType("datetime(6)");

            entity.Property(x => x.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(250);

            entity.HasIndex(x => new
            {
                x.Status,
                x.NextAttemptAtUtc,
                x.LockedUntilUtc
            });

        });

        modelBuilder.Entity<FeedingOutboxRow>(entity =>
        {
            entity.ToTable("nutrition_outbox");

            entity.HasKey(x => x.EventId);

            entity.Property(x => x.EventId)
                .HasColumnName("event_id")
                .ValueGeneratedNever();

            entity.Property(x => x.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Payload)
                .HasColumnName("payload")
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

            entity.Property(x => x.NextAttemptAtUtc)
                .HasColumnName("next_attempt_at_utc")
                .HasColumnType("datetime(6)");

            entity.HasIndex(x => new
            {
                x.PublishedAtUtc,
                x.NextAttemptAtUtc
            });
        });

        return modelBuilder;
    }
}
