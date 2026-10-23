namespace VetPax.Application.Modules.ClinicManagement.Ports;

public interface IVeterinarianIdentityDirectoryPort
{
    Task<bool> IsActiveVeterinarianAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}