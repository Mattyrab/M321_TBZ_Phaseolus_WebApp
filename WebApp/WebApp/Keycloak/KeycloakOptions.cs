namespace WebApp.Keycloak;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
