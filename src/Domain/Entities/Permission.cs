namespace DMS.Auth.Domain.Entities;

public class Permission
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    private Permission() { }

    public Permission(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }
}
