using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class KeycloakCredential
{
    [JsonPropertyName("temporary")]
    public bool? Temporary { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
