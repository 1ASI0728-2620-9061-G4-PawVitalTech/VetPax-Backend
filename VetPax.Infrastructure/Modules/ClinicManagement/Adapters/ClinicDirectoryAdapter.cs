using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

public sealed class ClinicDirectoryAdapter(
    VetPaxDbContext db) : IClinicDirectoryPort
{
    public async Task<ClinicDirectoryPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Clinics.AsNoTracking();

        var totalCount = await query.CountAsync(
            cancellationToken);

        var clinics = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.ClinicId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClinicDirectoryItem(
                c.ClinicId,
                c.Name,
                c.Address,
                c.Phone,
                c.ContactEmail
            ))
            .ToArrayAsync(cancellationToken);

        return new ClinicDirectoryPage(
            page,
            pageSize,
            totalCount,
            clinics);
    }
}