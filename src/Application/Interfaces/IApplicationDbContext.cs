using DMS.Auth.Domain.Entities;

namespace DMS.Auth.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
