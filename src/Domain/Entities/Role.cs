namespace DMS.Auth.Domain.Entities;

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public List<Permission> Permissions { get; private set; } = new();

    private Role() { }

    public Role(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    public void AddPermission(Permission permission)
    {
        if (!Permissions.Contains(permission))
            Permissions.Add(permission);
    }
}
