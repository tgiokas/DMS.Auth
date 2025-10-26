using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public record DepartmentRole(
    [property: JsonPropertyName("department")] Department Department,
    [property: JsonPropertyName("roles")] List<Guid> Roles);