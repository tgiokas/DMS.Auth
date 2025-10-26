using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class KeycloakUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("createdTimestamp")]
    public long CreatedTimestamp { get; set; }
    [JsonPropertyName("username")]
    public string UserName { get; set; } = string.Empty;
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

    [JsonPropertyName("credentials")]
    public IEnumerable<KeycloakCredential>? Credentials { get; set; }
    [JsonIgnore]
    public DateTime? CreatedAt
    {
        get
        {           
            return CreatedTimestamp > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(CreatedTimestamp).UtcDateTime
                : (DateTime?)null;
        }
    }
}

