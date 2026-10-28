using VetPax.Domain.Modules.MedicationTreatment.Exceptions;

namespace VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

public sealed class MedicationSchedule
{
    private readonly DayOfWeek[] _daysOfWeek;

    public MedicationScheduleId ScheduleId { get; }
    public TimeOnly LocalTime { get; }
    public MedicationDosage Dosage { get; }

    public IReadOnlyList<DayOfWeek> DaysOfWeek =>
        Array.AsReadOnly(_daysOfWeek);

    public MedicationSchedule(
        MedicationScheduleId scheduleId,
        TimeOnly localTime,
        IEnumerable<DayOfWeek> daysOfWeek,
        MedicationDosage dosage)
    {
        if (scheduleId.Value == Guid.Empty)
        {
            throw new MedicationDomainException(
                "A valid medication schedule ID is required.");
        }

        ArgumentNullException.ThrowIfNull(daysOfWeek);
        ArgumentNullException.ThrowIfNull(dosage);

        var days = daysOfWeek.Distinct().OrderBy(x => x).ToArray();

        if (days.Length == 0 ||
            days.Any(x => !Enum.IsDefined(x)))
        {
            throw new MedicationDomainException(
                "At least one valid weekday is required.");
        }

        ScheduleId = scheduleId;
        LocalTime = localTime;
        Dosage = dosage;
        _daysOfWeek = days;
    }

    public bool AppliesOn(DateOnly date)
    {
        return _daysOfWeek.Contains(date.DayOfWeek);
    }

    public bool ConflictsWith(MedicationSchedule other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return LocalTime == other.LocalTime &&
               _daysOfWeek.Intersect(
                   other._daysOfWeek).Any();
    }
}