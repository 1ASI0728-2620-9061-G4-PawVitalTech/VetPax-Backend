using System.Collections.Concurrent;
using VetPax.Application.Modules.ClinicManagement.Ports;

namespace VetPax.Tests.Acceptance.Support;

public sealed class TestVeterinarianDirectory
    : IVeterinarianIdentityDirectoryPort
{
    private readonly ConcurrentDictionary<Guid, byte>
        _allowed = new();

    public void Allow(Guid veterinarianId)
    {
        _allowed[veterinarianId] = 1;
    }

    public Task<bool> IsActiveVeterinarianAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _allowed.ContainsKey(userId));
    }
}