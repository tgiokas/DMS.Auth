namespace Authentication.Application.Dtos;
public class RoleAssignDto
{
    public required string Username { get; set; }
    public required List<RoleDto> Roles { get; set; }
}
