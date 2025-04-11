namespace Authentication.Application.Dtos;

public class KeycloakRole
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool? Composite { get; set; }
    public bool? ClientRole { get; set; }
    public string? ContainerId { get; set; }
}