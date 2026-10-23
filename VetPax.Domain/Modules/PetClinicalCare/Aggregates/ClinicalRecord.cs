
using VetPax.Domain.Modules.PetClinicalCare.Entities;
using VetPax.Domain.Modules.PetClinicalCare.Events;
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.PetClinicalCare.Aggregates;

public sealed class ClinicalRecord
{
    private readonly List<ClinicalAttention> _attentions = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public ClinicalRecordId ClinicalRecordId { get; }
    public PetId PetId { get; }
    public DateTime LastUpdatedAt { get; private set; }

    internal ClinicalRecord(
        ClinicalRecordId clinicalRecordId,
        PetId petId)
    {
        if (clinicalRecordId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid clinical record ID is required.");

        if (petId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid pet ID is required.");

        ClinicalRecordId = clinicalRecordId;
        PetId = petId;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void RegisterAttention(ClinicalAttention attention)
    {
        ArgumentNullException.ThrowIfNull(attention);

        attention.ValidateMandatoryData();

        if (_attentions.Any(a =>
            a.AttentionId == attention.AttentionId))
        {
            throw new InvalidOperationException(
                "Clinical attention already exists.");
        }

        _attentions.Add(attention);

        LastUpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(
            new ClinicalAttentionRegisteredEvent(
                ClinicalRecordId,
                PetId,
                attention.AttentionId,
                attention.ClinicId,
                attention.VeterinarianId,
                LastUpdatedAt
            )
        );

        _domainEvents.Add(
            new ClinicalRecordUpdatedEvent(
                ClinicalRecordId,
                PetId,
                LastUpdatedAt
            )
        );
    }

    public IReadOnlyList<ClinicalAttention>
        GetAttentionsChronologically()
    {
        return _attentions
            .OrderBy(a => a.AttentionDate)
            .ToArray();
    }

    public bool HasAttentions()
    {
        return _attentions.Count > 0;
    }

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }
    private ClinicalRecord()
    {
    }

    public IReadOnlyCollection<ClinicalAttention> Attentions
        => _attentions.AsReadOnly();
}
