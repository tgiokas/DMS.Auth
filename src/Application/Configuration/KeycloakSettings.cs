using Microsoft.Extensions.Configuration;

namespace Authentication.Application.Configuration;

public class KeycloakSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = false;

    public static KeycloakSettings BindFromConfiguration(IConfiguration configuration)
    {
        return new KeycloakSettings
        {
            BaseUrl = configuration["KEYCLOAK_BASEURL"]
                ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_BASEURL is not set."),

            Realm = configuration["KEYCLOAK_REALM"]
                ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_REALM is not set."),

            ClientId = configuration["KEYCLOAK_CLIENTID"]
                ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_CLIENTID is not set."),

            ClientSecret = configuration["KEYCLOAK_CLIENTSECRET"]
                ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_CLIENTSECRET is not set."),

            Authority = configuration["KEYCLOAK_AUTHORITY"]
                ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_AUTHORITY is not set."),

            RedirectUri = configuration["KEYCLOAK_REDIRECTURI"]
                ?? throw new ArgumentNullException(nameof(configuration), "KEYCLOAK_REDIRECTURI is not set."),

            RequireHttpsMetadata = bool.Parse(
                configuration["Keycloak:RequireHttpsMetadata"] ?? "false")
        };
    }
}
