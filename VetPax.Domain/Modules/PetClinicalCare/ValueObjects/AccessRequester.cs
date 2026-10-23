namespace VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

public sealed class AccessRequester
{
    public Guid UserId { get; }
    public RequesterRole Role { get; }
    public ClinicId? ClinicId { get; }

    public AccessRequester(
        Guid userId,
        RequesterRole role,
        ClinicId? clinicId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "A valid user ID is required.");

        if (!Enum.IsDefined(role))
            throw new ArgumentException(
                "Invalid requester role.");

        UserId = userId;
        Role = role;
        ClinicId = clinicId;
    }

    public bool IsOwner()
        => Role == RequesterRole.Owner;

    public bool IsVeterinarian()
        => Role == RequesterRole.Veterinarian;
}