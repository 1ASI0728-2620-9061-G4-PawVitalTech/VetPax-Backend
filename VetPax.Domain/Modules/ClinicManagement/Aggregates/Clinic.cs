
using VetPax.Domain.Modules.ClinicManagement.Events;
using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.ClinicManagement.Aggregates;

public sealed class Clinic
{
    private readonly List<AdministratorId> _administrators = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public ClinicId ClinicId { get; private set; }

    public ClinicGeneralData GeneralData { get; private set; } = null!;

    public OpeningSchedule OpeningSchedule { get; private set; } = null!;

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<AdministratorId> Administrators =>
        _administrators.AsReadOnly();

    private Clinic() { }

    public Clinic(
        ClinicId clinicId,
        AdministratorId administratorId,
        ClinicGeneralData generalData,
        OpeningSchedule openingSchedule)
    {
        if (clinicId.Value == Guid.Empty ||
            administratorId.Value == Guid.Empty)
            throw new ArgumentException(
                "Valid clinic and administrator IDs are required.");

        ArgumentNullException.ThrowIfNull(generalData);
        ArgumentNullException.ThrowIfNull(openingSchedule);

        generalData.ValidateMandatoryData();
        openingSchedule.ValidateConsistency();

        ClinicId = clinicId;
        GeneralData = generalData;
        OpeningSchedule = openingSchedule;

        _administrators.Add(administratorId);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsManagedBy(Guid userId) =>
        userId != Guid.Empty &&
        _administrators.Any(a => a.Value == userId);

    public OpeningSchedule GetOpeningSchedule() =>
        OpeningSchedule;

    public void UpdateProfile(
        ClinicGeneralData generalData,
        OpeningSchedule openingSchedule)
    {
        ArgumentNullException.ThrowIfNull(generalData);
        ArgumentNullException.ThrowIfNull(openingSchedule);

        generalData.ValidateMandatoryData();
        openingSchedule.ValidateConsistency();

        GeneralData = generalData;
        OpeningSchedule = openingSchedule;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(
            new ClinicProfileUpdatedEvent(
                ClinicId,
                GeneralData,
                OpeningSchedule,
                UpdatedAt));
    }

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
