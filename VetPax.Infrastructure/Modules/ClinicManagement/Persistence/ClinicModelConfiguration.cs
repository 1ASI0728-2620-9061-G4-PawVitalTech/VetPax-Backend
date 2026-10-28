
using Microsoft.EntityFrameworkCore;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Persistence;

public static class ClinicModelConfiguration
{
    public static void ConfigureClinicManagement(
        this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClinicRow>(entity =>
        {
            entity.ToTable("clinics");
            entity.HasKey(x => x.ClinicId);

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id");

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Address)
                .HasColumnName("address")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(40);

            entity.Property(x => x.ContactEmail)
                .HasColumnName("contact_email")
                .HasMaxLength(254);

            entity.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<ClinicAdministratorRow>(entity =>
        {
            entity.ToTable("clinic_administrators");

            entity.HasKey(x => new
            {
                x.ClinicId,
                x.AdministratorId
            });

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id");

            entity.Property(x => x.AdministratorId)
                .HasColumnName("administrator_id");

            entity.HasOne<ClinicRow>()
                .WithMany()
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicOpeningHourRow>(entity =>
        {
            entity.ToTable("clinic_opening_hours");
            entity.HasKey(x => x.OpeningHoursId);

            entity.Property(x => x.OpeningHoursId)
                .HasColumnName("opening_hours_id");

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id");

            entity.Property(x => x.DayOfWeek)
                .HasColumnName("day_of_week")
                .HasConversion<string>()
                .HasMaxLength(16);

            entity.Property(x => x.OpenTime)
                .HasColumnName("open_time")
                .HasColumnType("time(6)");

            entity.Property(x => x.CloseTime)
                .HasColumnName("close_time")
                .HasColumnType("time(6)");

            entity.HasOne<ClinicRow>()
                .WithMany()
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicVeterinarianRow>(entity =>
        {
            entity.ToTable("clinic_veterinarians");

            entity.HasKey(x => new
            {
                x.ClinicId,
                x.VeterinarianId
            });

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id");

            entity.Property(x => x.VeterinarianId)
                .HasColumnName("veterinarian_id");

            entity.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            entity.HasOne<ClinicRow>()
                .WithMany()
                .HasForeignKey(x => x.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
