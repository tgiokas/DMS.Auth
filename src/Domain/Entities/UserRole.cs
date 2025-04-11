namespace Authentication.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
}
