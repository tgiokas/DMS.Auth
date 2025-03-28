using Microsoft.EntityFrameworkCore;

using DMS.Auth.Domain.Entities;
using DMS.Auth.Application.Interfaces;

namespace DMS.Auth.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }
    public required DbSet<Role> Roles { get; set; }

    public required DbSet<UserTotpSecret> UserTotpSecrets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User Entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username).HasMaxLength(200);
            entity.Property(u => u.Email).HasMaxLength(300);                
        });

        //Configure User Entity
        modelBuilder.Entity<UserTotpSecret>(entity =>
        {
            entity.ToTable("UserTotpSecrets");
            entity.HasKey(u => u.Id);

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Base32Secret).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
