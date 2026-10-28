using VetPax.Application.Modules.ClinicManagement.Ports;

namespace VetPax.Application.Modules.ClinicManagement.UseCases.ListClinics;

public sealed class ListClinicsHandler(
    IClinicDirectoryPort directory)
{
    public Task<ClinicDirectoryPage> HandleAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > 100000)
        {
            throw new ArgumentException(
                "Page must be between 1 and 100000.");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            throw new ArgumentException(
                "Page size must be between 1 and 50.");
        }

        return directory.GetPageAsync(
            page,
            pageSize,
            cancellationToken);
    }
}