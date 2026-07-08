namespace Authentication.Application.Dtos
{
    public class UserCreateWithRoleDto
    {
        public required UserCreateDto User { get; set; }
        public required RoleDto Role { get; set; }
    }
}
