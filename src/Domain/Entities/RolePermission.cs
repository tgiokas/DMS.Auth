namespace Authentication.Domain.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public required Guid KeycloakRoleId { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty; 
    public bool Allowed { get; set; }
    public List<string> EndPoints { get; set; } = new();
    public List<string> Urls { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}