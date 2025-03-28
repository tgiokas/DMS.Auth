namespace DMS.Auth.Application.Dtos;

public class KeycloakRoleDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool? Composite { get; set; }
    public bool? ClientRole { get; set; }
    public string? ContainerId { get; set; }
}