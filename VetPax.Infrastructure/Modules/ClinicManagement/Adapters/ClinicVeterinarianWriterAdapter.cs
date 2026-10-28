using Microsoft.EntityFrameworkCore;
using VetPax.Application.Modules.ClinicManagement.Ports;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Infrastructure.Modules.ClinicManagement.Persistence;
using VetPax.Infrastructure.Persistence;

namespace VetPax.Infrastructure.Modules.ClinicManagement.Adapters;

public sealed class ClinicVeterinarianWriterAdapter(
    VetPaxDbContext db) : IClinicVeterinarianWriterPort
{
    public async Task LinkAsync(
        ClinicId clinicId,
        Guid veterinarianId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.ClinicVeterinarians
            .SingleOrDefaultAsync(
                x => x.ClinicId == clinicId.Value &&
                     x.VeterinarianId == veterinarianId,
                cancellationToken);

        if (row is null)
        {
            db.ClinicVeterinarians.Add(
                new ClinicVeterinarianRow
                {
                    ClinicId = clinicId.Value,
                    VeterinarianId = veterinarianId,
                    IsActive = true
                });
        }
        else
        {
            row.IsActive = true;
        }
    }
}