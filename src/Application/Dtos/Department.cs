using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public record Department(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);