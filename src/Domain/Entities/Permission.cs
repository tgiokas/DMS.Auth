namespace Authentication.Domain.Entities;

public class Permission
{
    public Guid Id { get; private set; }
    public required string Name { get; set; }
}