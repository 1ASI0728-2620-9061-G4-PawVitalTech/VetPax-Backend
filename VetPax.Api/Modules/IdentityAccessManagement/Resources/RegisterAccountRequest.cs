using System.ComponentModel.DataAnnotations;

namespace VetPax.Api.Modules.IdentityAccessManagement.Resources;

public sealed record RegisterAccountRequest(
    [property: Required]
    [property: EmailAddress]
    string Email,

    [property: Required]
    string Password);