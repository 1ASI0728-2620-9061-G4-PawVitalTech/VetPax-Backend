namespace VetPax.Api.Modules.IdentityAccessManagement.Resources;

public sealed record MyPermissionsResponse(
    IReadOnlyList<string> Permissions);