namespace Authentication.Domain.Entities;

public class BusinessRule
{
    public int Id { get; set; }
    public int DepartmentId { get; set; } 
    public Guid KeycloakRoleId { get; set; }
    public string HttpMethod { get; set; } = string.Empty;   // e.g., "POST"
    public string PathPattern { get; set; } = string.Empty;  // e.g., "/api/signature/*"
    public bool Allowed { get; set; }                        // true = allow, false = deny
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}