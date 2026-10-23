using VetPax.Domain.Modules.ClinicManagement.ValueObjects;
using VetPax.Domain.Shared.Events;

namespace VetPax.Domain.Modules.ClinicManagement.Events;

public sealed record ClinicProfileUpdatedEvent(
    ClinicId ClinicId,
    ClinicGeneralData GeneralData,
    OpeningSchedule OpeningSchedule,
    DateTime OccurredAt
) : IDomainEvent;