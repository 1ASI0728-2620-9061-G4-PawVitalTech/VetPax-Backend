namespace VetPax.Infrastructure.Modules.ClinicManagement.Persistence;

public sealed class ClinicRow
{
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Phone { get; set; }
    public string? ContactEmail { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class ClinicAdministratorRow
{
    public Guid ClinicId { get; set; }
    public Guid AdministratorId { get; set; }
}

public sealed class ClinicOpeningHourRow
{
    public Guid OpeningHoursId { get; set; }
    public Guid ClinicId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
}

public sealed class ClinicVeterinarianRow
{
    public Guid ClinicId { get; set; }
    public Guid VeterinarianId { get; set; }
    public bool IsActive { get; set; }
}