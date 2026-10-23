
using VetPax.Domain.Modules.PetClinicalCare.ValueObjects;

namespace VetPax.Domain.Modules.PetClinicalCare.Entities;

public sealed class ClinicalAttention
{
    public AttentionId AttentionId { get; }
    public ClinicId ClinicId { get; }
    public VeterinarianId VeterinarianId { get; }
    public DateTime AttentionDate { get; }

    public string Reason { get; }
    public string Diagnosis { get; }
    public string TreatmentSummary { get; }
    public string? Observations { get; }

    private readonly List<ClinicalIndicator> _indicators = new();

    public IReadOnlyList<ClinicalIndicator> Indicators
        => _indicators.AsReadOnly();
    
    public ClinicalAttention(
        AttentionId attentionId,
        ClinicId clinicId,
        VeterinarianId veterinarianId,
        DateTime attentionDate,
        string reason,
        string diagnosis,
        string treatmentSummary,
        string? observations = null,
        IEnumerable<ClinicalIndicator>? indicators = null)
    {
        AttentionId = attentionId;
        ClinicId = clinicId;
        VeterinarianId = veterinarianId;
        AttentionDate = attentionDate;

        Reason = reason;
        Diagnosis = diagnosis;
        TreatmentSummary = treatmentSummary;
        Observations = observations;

        _indicators.AddRange(
            indicators ?? Enumerable.Empty<ClinicalIndicator>()
        );

        ValidateMandatoryData();
    }

    public void ValidateMandatoryData()
    {
        if (AttentionId.Value == Guid.Empty ||
            ClinicId.Value == Guid.Empty ||
            VeterinarianId.Value == Guid.Empty)
            throw new ArgumentException(
                "Valid clinical identifiers are required.");

        if (AttentionDate == default)
            throw new ArgumentException(
                "Attention date is required.");

        if (string.IsNullOrWhiteSpace(Reason) ||
            string.IsNullOrWhiteSpace(Diagnosis) ||
            string.IsNullOrWhiteSpace(TreatmentSummary))
            throw new ArgumentException(
                "Required clinical information is missing.");
    }

    public bool HasIndicators()
    {
        return Indicators.Count > 0;
    }

    public ClinicalIndicator? FindIndicator(string name)
    {
        return Indicators.FirstOrDefault(i =>
            string.Equals(i.Name, name,
                StringComparison.OrdinalIgnoreCase));
    }
    
    private ClinicalAttention()
    {
        Reason = string.Empty;
        Diagnosis = string.Empty;
        TreatmentSummary = string.Empty;
    }
    
}
