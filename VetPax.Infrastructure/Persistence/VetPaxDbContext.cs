
using Microsoft.EntityFrameworkCore;
using VetPax.Domain.Modules.PetClinicalCare.Aggregates;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Infrastructure.Modules.ClinicManagement.Persistence;
using VetPax.Infrastructure.Modules.PetClinicalCare.Persistence;

namespace VetPax.Infrastructure.Persistence;

public sealed class VetPaxDbContext : DbContext
{
    public VetPaxDbContext(
        DbContextOptions<VetPaxDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<ClinicalRecord> ClinicalRecords
        => Set<ClinicalRecord>();
    
    public DbSet<ClinicRow> Clinics => Set<ClinicRow>();

    public DbSet<ClinicAdministratorRow> ClinicAdministrators =>
        Set<ClinicAdministratorRow>();

    public DbSet<ClinicOpeningHourRow> ClinicOpeningHours =>
        Set<ClinicOpeningHourRow>();

    public DbSet<ClinicVeterinarianRow> ClinicVeterinarians =>
        Set<ClinicVeterinarianRow>();
    
    public DbSet<PetClinicLinkRow> PetClinicLinks =>
        Set<PetClinicLinkRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pet>(pet =>
        {
            pet.ToTable("pets");

            pet.HasKey(p => p.PetId);

            pet.Property(p => p.PetId)
                .HasConversion(
                    id => id.Value,
                    value => new PetId(value))
                .ValueGeneratedNever();

            pet.Property(p => p.OwnerId)
                .HasConversion(
                    id => id.Value,
                    value => new OwnerId(value));

            pet.Property(p => p.Name)
                .HasMaxLength(150)
                .IsRequired();

            pet.Property(p => p.Species)
                .HasConversion<string>()
                .HasMaxLength(20);

            pet.Property(p => p.Breed)
                .HasMaxLength(100);

            pet.Property(p => p.Sex)
                .HasConversion<string>()
                .HasMaxLength(20);

            pet.Property(p => p.BirthDate);
            pet.Property(p => p.RegisteredAt);

            pet.HasIndex(p => p.OwnerId);
        });

        modelBuilder.Entity<ClinicalRecord>(record =>
        {
            record.ToTable("clinical_records");

            record.HasKey(r => r.ClinicalRecordId);

            record.Property(r => r.ClinicalRecordId)
                .HasConversion(
                    id => id.Value,
                    value => new ClinicalRecordId(value))
                .ValueGeneratedNever();

            record.Property(r => r.PetId)
                .HasConversion(
                    id => id.Value,
                    value => new PetId(value));

            record.Property(r => r.LastUpdatedAt);

            record.HasIndex(r => r.PetId).IsUnique();

            record.OwnsMany(r => r.Attentions, attention =>
            {
                attention.ToTable("clinical_attentions");

                attention.WithOwner()
                    .HasForeignKey("ClinicalRecordId");

                attention.Property<ClinicalRecordId>(
                        "ClinicalRecordId")
                    .HasConversion(
                        id => id.Value,
                        value => new ClinicalRecordId(value));

                attention.HasKey(a => a.AttentionId);

                attention.Property(a => a.AttentionId)
                    .HasConversion(
                        id => id.Value,
                        value => new AttentionId(value))
                    .ValueGeneratedNever();

                attention.Property(a => a.ClinicId)
                    .HasConversion(
                        id => id.Value,
                        value => new ClinicId(value));

                attention.Property(a => a.VeterinarianId)
                    .HasConversion(
                        id => id.Value,
                        value => new VeterinarianId(value));

                attention.Property(a => a.AttentionDate);

                attention.Property(a => a.Reason)
                    .HasMaxLength(500)
                    .IsRequired();

                attention.Property(a => a.Diagnosis)
                    .HasMaxLength(1000)
                    .IsRequired();

                attention.Property(a => a.TreatmentSummary)
                    .HasMaxLength(2000)
                    .IsRequired();

                attention.Property(a => a.Observations)
                    .HasMaxLength(2000);

                attention.OwnsMany(a => a.Indicators, indicator =>
                {
                    indicator.ToTable("clinical_indicators");

                    indicator.WithOwner()
                        .HasForeignKey("AttentionId");

                    indicator.Property<AttentionId>("AttentionId")
                        .HasConversion(
                            id => id.Value,
                            value => new AttentionId(value));

                    indicator.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    indicator.HasKey("Id");

                    indicator.Property(i => i.Name)
                        .HasMaxLength(100)
                        .IsRequired();

                    indicator.Property(i => i.Value)
                        .HasPrecision(18, 4);

                    indicator.Property(i => i.Unit)
                        .HasMaxLength(30)
                        .IsRequired();
                });

                attention.Navigation(a => a.Indicators)
                    .HasField("_indicators")
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            record.Navigation(r => r.Attentions)
                .HasField("_attentions")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        modelBuilder.ConfigureClinicManagement();
        
        modelBuilder.Entity<PetClinicLinkRow>(entity =>
        {
            entity.ToTable("pet_clinic_links");

            entity.HasKey(x => new
            {
                x.PetId,
                x.ClinicId
            });

            entity.Property(x => x.PetId)
                .HasColumnName("pet_id");

            entity.Property(x => x.ClinicId)
                .HasColumnName("clinic_id");

            entity.Property(x => x.IsActive)
                .HasColumnName("is_active");

            entity.Property(x => x.LinkedAt)
                .HasColumnName("linked_at")
                .HasColumnType("datetime(6)");
        });

    }
    
}
