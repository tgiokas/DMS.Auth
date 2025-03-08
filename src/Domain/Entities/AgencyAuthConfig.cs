namespace DMS.Auth.Domain.Entities;

public class AgencyAuthConfig
{
    public string AgencyId { get; set; }  // e.g., "agency1"
    public string KeycloakUrl { get; set; }  // e.g., "https://auth.agency1.com"
    public string Realm { get; set; }  // e.g., "agency1-realm"
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public bool UseIdToken { get; set; }
    public bool UseAccessToken { get; set; }
}
