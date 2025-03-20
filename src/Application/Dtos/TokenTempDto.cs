using System.Text.Json.Serialization;

namespace DMS.Auth.Application.Dtos;

public class TokenTempDto
{
    [JsonPropertyName("code")]
    public string Code { get; set; }
}

