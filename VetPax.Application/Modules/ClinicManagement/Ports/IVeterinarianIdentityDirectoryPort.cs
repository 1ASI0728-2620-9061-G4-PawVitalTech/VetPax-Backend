namespace VetPax.Application.Modules.ClinicManagement.Ports;

public interface IVeterinarianIdentityDirectoryPort
{
    Task<VeterinarianPublicProfile?> GetVeterinarianProfileAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsActiveVeterinarianAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
public sealed record VeterinarianPublicProfile(
    string? FullName, string? Specialty, string? LicenseNumber);