using VetPax.Domain.Modules.MedicationTreatment.ValueObjects;

namespace VetPax.Domain.Modules.MedicationTreatment.Entities;

public sealed record DoseAdministration(
    MedicationAdministrationId AdministrationId,
    MedicationDoseId DoseId,
    MedicationOwnerId AdministeredBy,
    DateTime AdministeredAtUtc,
    DateTime RecordedAtUtc);