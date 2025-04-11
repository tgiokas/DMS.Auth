using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class KeycloakUser
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("createdTimestamp")]
    public long CreatedTimestamp { get; set; }
    [JsonPropertyName("username")]
    public string? UserName { get; set; }
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
    [JsonPropertyName("totp")]
    public bool? Totp { get; set; }
    [JsonPropertyName("emailVerified")]
    public bool? EmailVerified { get; set; }
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("notBefore")]
    public int? NotBefore { get; set; }
    [JsonPropertyName("access")]
    public UserAccess? Access { get; set; }

    [JsonPropertyName("requiredActions")]
    public IEnumerable<string>? RequiredActions { get; set; }
    [JsonPropertyName("disableableCredentialTypes")]
    public IEnumerable<string>? DisableableCredentialTypes { get; set; }

    [JsonPropertyName("credentials")]
    public IEnumerable<KeycloakCredential>? Credentials { get; set; }
}

public class UserAccess
{
    [JsonPropertyName("manageGroupMembership")]
    public bool? ManageGroupMembership { get; set; }
    [JsonPropertyName("view")]
    public bool? View { get; set; }
    [JsonPropertyName("mapRoles")]
    public bool? MapRoles { get; set; }
    [JsonPropertyName("impersonate")]
    public bool? Impersonate { get; set; }
    [JsonPropertyName("manage")]
    public bool? Manage { get; set; }
}