namespace Authentication.Application.Dtos;

public class UserCreateWithRolesDto
{
    public required UserCreateDto User { get; set; }
    public required List<RoleDto> Roles { get; set; }
}
