using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class KeycloakClient
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;
 
}