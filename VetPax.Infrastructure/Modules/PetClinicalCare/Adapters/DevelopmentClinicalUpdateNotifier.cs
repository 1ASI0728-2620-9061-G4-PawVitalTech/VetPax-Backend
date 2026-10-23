using Microsoft.Extensions.Logging;
using VetPax.Application.Modules.PetClinicalCare.Ports;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Infrastructure.Modules.PetClinicalCare.Adapters;

public sealed class DevelopmentClinicalUpdateNotifier
    : IClinicalUpdateNotifier
{
    private readonly ILogger<DevelopmentClinicalUpdateNotifier>
        _logger;

    public DevelopmentClinicalUpdateNotifier(
        ILogger<DevelopmentClinicalUpdateNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyClinicalRecordUpdatedAsync(
        PetId petId,
        ClinicalRecordId clinicalRecordId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "[DEV] Clinical record updated. PetId: {PetId}, RecordId: {RecordId}",
            petId.Value,
            clinicalRecordId.Value);

        return Task.CompletedTask;
    }
}