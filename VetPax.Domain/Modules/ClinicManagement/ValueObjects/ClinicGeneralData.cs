using System.Net.Mail;

namespace VetPax.Domain.Modules.ClinicManagement.ValueObjects;

public sealed record ClinicGeneralData
{
    public string Name { get; }
    public string Address { get; }
    public string? Phone { get; }
    public string? ContactEmail { get; }

    public ClinicGeneralData(
        string name,
        string address,
        string? phone = null,
        string? contactEmail = null)
    {
        Name = name?.Trim() ?? "";
        Address = address?.Trim() ?? "";
        Phone = string.IsNullOrWhiteSpace(phone)
            ? null : phone.Trim();
        ContactEmail = string.IsNullOrWhiteSpace(contactEmail)
            ? null : contactEmail.Trim();

        ValidateMandatoryData();
    }

    public void ValidateMandatoryData()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException(
                "Clinic name is required.");

        if (string.IsNullOrWhiteSpace(Address))
            throw new ArgumentException(
                "Clinic address is required.");

        if (ContactEmail is not null &&
            !MailAddress.TryCreate(ContactEmail, out _))
            throw new ArgumentException(
                "Invalid contact email.");
    }
}