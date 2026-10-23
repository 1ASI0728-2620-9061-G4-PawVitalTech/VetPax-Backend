namespace VetPax.Application.Common.Ports;

public interface IUnitOfWork
{
    Task CommitAsync(
        CancellationToken cancellationToken = default
    );
}