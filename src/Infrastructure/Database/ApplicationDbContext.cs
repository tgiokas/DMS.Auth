using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Database;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }   
    public required DbSet<BusinessRule> BusinessRules { get; set; }
    public required DbSet<UserTotpSecret> UserTotpSecrets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User Entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(300);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.KeycloakUserId).IsUnique();
        });

        //Configure UserTotpSecret Entity
        modelBuilder.Entity<UserTotpSecret>(entity =>
        {
            entity.ToTable("UserTotpSecrets");
            entity.HasKey(u => u.Id);
            entity.Property(x => x.KeycloakUserId).IsRequired();
            entity.Property(x => x.Base32Secret).IsRequired();
            entity.HasIndex(x => x.KeycloakUserId).IsUnique();
        });

        // Configure BusinessRule Entity
        modelBuilder.Entity<BusinessRule>(entity =>
        {
            entity.ToTable("BusinessRules");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.DepartmentId).IsRequired();
            entity.Property(b => b.KeycloakRoleId).IsRequired();
            entity.Property(b => b.HttpMethod).IsRequired().HasMaxLength(10);
            entity.Property(b => b.PathPattern).IsRequired().HasMaxLength(500);
            entity.HasIndex(b => new { b.DepartmentId, b.KeycloakRoleId, b.HttpMethod });
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
