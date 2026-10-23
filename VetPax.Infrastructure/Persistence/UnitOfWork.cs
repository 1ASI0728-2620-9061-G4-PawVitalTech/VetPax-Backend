using VetPax.Application.Common.Ports;

namespace VetPax.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly VetPaxDbContext _context;

    public UnitOfWork(VetPaxDbContext context)
    {
        _context = context;
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}