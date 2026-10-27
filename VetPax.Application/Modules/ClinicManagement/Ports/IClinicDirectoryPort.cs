namespace VetPax.Application.Modules.ClinicManagement.Ports;

public sealed record ClinicDirectoryItem(
    Guid ClinicId,
    string Name,
    string Address,
    string? Phone,
    string? ContactEmail
);

public sealed record ClinicDirectoryPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ClinicDirectoryItem> Items
);

public interface IClinicDirectoryPort
{
    Task<ClinicDirectoryPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}