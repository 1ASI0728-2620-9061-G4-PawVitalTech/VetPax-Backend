namespace VetPax.Infrastructure.Modules.IdentityAccessManagement.Configuration;

public sealed class KeycloakOptions
{
    public string BaseUrl { get; set; } = "";
    public string Realm { get; set; } = "";
    public string Audience { get; set; } = "";
    public string AdminClientId { get; set; } = "";
    public string AdminClientSecret { get; set; } = "";

    public string Authority =>
        $"{BaseUrl.TrimEnd('/')}/realms/{Realm}";
}