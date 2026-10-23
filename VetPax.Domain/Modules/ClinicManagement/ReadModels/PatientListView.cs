using VetPax.Domain.Modules.ClinicManagement.ValueObjects;

namespace VetPax.Domain.Modules.ClinicManagement.ReadModels;

public sealed class PatientListView
{
    public ClinicId ClinicId { get; }

    public IReadOnlyList<PatientSummary> Patients { get; }

    public bool HasPatients => Patients.Count > 0;

    public PatientListView(
        ClinicId clinicId,
        IEnumerable<PatientSummary> patients)
    {
        ArgumentNullException.ThrowIfNull(patients);

        ClinicId = clinicId;
        Patients = patients.ToArray();
    }
}