namespace VetPax.Domain.Modules.ClinicManagement.ValueObjects;

public enum ClinicRequesterRole
{
    Veterinarian,
    ClinicAdministrator
}

public sealed record ClinicAccessRequester
{
    public Guid UserId { get; }
    public ClinicRequesterRole Role { get; }
    public ClinicId ClinicId { get; }

    public ClinicAccessRequester(
        Guid userId,
        ClinicRequesterRole role,
        ClinicId clinicId)
    {
        if (userId == Guid.Empty ||
            clinicId.Value == Guid.Empty ||
            !Enum.IsDefined(role))
            throw new ArgumentException(
                "Invalid clinic requester.");

        UserId = userId;
        Role = role;
        ClinicId = clinicId;
    }

    public bool IsAdministrator() =>
        Role == ClinicRequesterRole.ClinicAdministrator;

    public bool IsVeterinarian() =>
        Role == ClinicRequesterRole.Veterinarian;
}