namespace VetPax.Infrastructure.Modules.PetClinicalCare.Persistence;

public sealed class PetClinicLinkRow
{
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public bool IsActive { get; set; }
    public DateTime LinkedAt { get; set; }
}