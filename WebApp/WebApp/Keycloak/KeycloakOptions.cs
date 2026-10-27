namespace WebApp.Keycloak;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    // The backend reaches Keycloak over the in-cluster Service DNS name (Authority
    // above), but the user's browser can only reach it through the dev port-forward.
    // Front-channel redirects get rewritten from Authority to this address.
    public string? PublicAuthority { get; set; }
}
