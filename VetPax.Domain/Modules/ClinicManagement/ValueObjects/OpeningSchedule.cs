namespace VetPax.Domain.Modules.ClinicManagement.ValueObjects;

public sealed class OpeningSchedule
{
    private readonly List<DailyOpeningHours> _days;

    public IReadOnlyList<DailyOpeningHours> Days =>
        _days.AsReadOnly();

    public OpeningSchedule(
        IEnumerable<DailyOpeningHours> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        _days = days.ToList();
        ValidateConsistency();
    }

    public void ValidateConsistency()
    {
        if (_days.Count > 7)
            throw new ArgumentException(
                "A schedule supports up to seven intervals.");

        for (var i = 0; i < _days.Count; i++)
        {
            if (_days[i] is null || !_days[i].IsValid())
                throw new ArgumentException(
                    "Invalid daily opening hours.");

            for (var j = i + 1; j < _days.Count; j++)
            {
                if (_days[i].OverlapsWith(_days[j]))
                    throw new ArgumentException(
                        "Clinic opening hours cannot overlap.");
            }
        }
    }

    public bool IsOpenAt(
        DayOfWeek dayOfWeek,
        TimeOnly time)
    {
        return _days.Any(d =>
            d.DayOfWeek == dayOfWeek &&
            time >= d.OpenTime &&
            time < d.CloseTime);
    }
}