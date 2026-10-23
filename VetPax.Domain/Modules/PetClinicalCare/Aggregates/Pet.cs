using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;
using VetPax.Domain.Modules.PetClinicalCare.Events;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.PetClinicalCare.Aggregates;

public sealed class Pet
{
    public PetId PetId { get; }
    public OwnerId OwnerId { get; }

    public string Name { get; }
    public Species Species { get; }
    public string? Breed { get; }
    public Sex? Sex { get; }

    public DateOnly BirthDate { get; }
    public DateTime RegisteredAt { get; }

    internal Pet(
        PetId petId,
        OwnerId ownerId,
        string name,
        Species species,
        DateOnly birthDate,
        string? breed = null,
        Sex? sex = null,
        DateTime? registeredAt = null)
    {
        if (petId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid pet ID is required.",
                nameof(petId));

        if (ownerId.Value == Guid.Empty)
            throw new ArgumentException(
                "A valid owner ID is required.",
                nameof(ownerId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Pet name is required.",
                nameof(name));

        if (!Enum.IsDefined(species))
            throw new ArgumentException(
                "Invalid pet species.",
                nameof(species));

        if (sex.HasValue && !Enum.IsDefined(sex.Value))
            throw new ArgumentException(
                "Invalid pet sex.",
                nameof(sex));

        if (birthDate == default ||
            birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException(
                "Birth date must be valid and cannot be in the future.",
                nameof(birthDate));

        PetId = petId;
        OwnerId = ownerId;
        Name = name.Trim();
        Species = species;
        BirthDate = birthDate;

        Breed = string.IsNullOrWhiteSpace(breed)
            ? null
            : breed.Trim();

        Sex = sex;
        RegisteredAt = registeredAt ?? DateTime.UtcNow;
    }

    public bool IsOwnedBy(OwnerId ownerId)
    {
        return OwnerId == ownerId;
    }

    public int CalculateAge(DateOnly referenceDate)
    {
        if (referenceDate < BirthDate)
            throw new ArgumentException(
                "Reference date cannot be before birth date.",
                nameof(referenceDate));

        int years = referenceDate.Year - BirthDate.Year;

        if (BirthDate.AddYears(years) > referenceDate)
            years--;

        return years;
    }
    
    // Pending domain events
    private readonly List<IDomainEvent> _domainEvents = new();

    // Register a domain event
    internal void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    // Retrieve and clear pending domain events
    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        return events;
    }
    // Used only by persistence
    private Pet()
    {
        Name = string.Empty;
    }

}
