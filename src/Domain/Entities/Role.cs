using System.ComponentModel.DataAnnotations;

namespace Authentication.Domain.Entities;

public class Role
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public List<Permission> Permissions { get; private set; } = new();

    public void AddPermission(Permission permission)
    {
        if (!Permissions.Contains(permission))
            Permissions.Add(permission);
    }
}
