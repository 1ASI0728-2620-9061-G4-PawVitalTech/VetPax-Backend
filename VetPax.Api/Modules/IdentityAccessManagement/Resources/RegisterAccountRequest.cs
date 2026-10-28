using System.ComponentModel.DataAnnotations;

namespace VetPax.Api.Modules.IdentityAccessManagement.Resources;

public sealed record RegisterAccountRequest(
    [param: Required(AllowEmptyStrings = false)]
    [param: MinLength(2)]
    [param: MaxLength(80)]
    string FirstName,

    [param: Required(AllowEmptyStrings = false)]
    [param: MinLength(2)]
    [param: MaxLength(80)]
    string LastName,

    [param: Required(AllowEmptyStrings = false)]
    [param: EmailAddress]
    string Email,

    [param: Required(AllowEmptyStrings = false)]
    [param: MinLength(8)]
    [param: MaxLength(128)]
    string Password);