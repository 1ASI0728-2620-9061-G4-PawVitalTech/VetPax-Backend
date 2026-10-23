using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

public sealed class ClinicMembershipAdapter(
    VetPaxDbContext db) : IClinicMembershipPort
{
    public async Task<bool> IsActiveVeterinarianAsync(
        Guid userId,
        ClinicId clinicId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty ||
            clinicId.Value == Guid.Empty)
            return false;

        return await db.ClinicVeterinarians
            .AsNoTracking()
            .AnyAsync(x =>
                    x.VeterinarianId == userId &&
                    x.ClinicId == clinicId.Value &&
                    x.IsActive,
                cancellationToken);
    }
}