using VetPax.Application.Modules.AdherenceGamification.Ports;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.AdherenceGamification.Adapters;

public sealed class AdherenceUnitOfWork : IAdherenceUnitOfWork
{
    private readonly VetPaxDbContext _db;

    public AdherenceUnitOfWork(VetPaxDbContext db)
    {
        _db = db;
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}