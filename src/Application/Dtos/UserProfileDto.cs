using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class UserProfileDto
{
    // Keycloak user properties
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool? EmailVerified { get; set; }
    public bool? Enabled { get; set; }

    // Local user properties
    [JsonIgnore]
    public bool Deleted { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsAdmin { get; set; }    
    public string? MfaMethod { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Keycloak user properties
    public Dictionary<string, string[]>? Attributes { get; set; }
    public List<RoleProfileDto> Roles { get; set; } = new();

}
