using Microsoft.EntityFrameworkCore;

using Authentication.Application.Interfaces;
using Authentication.Domain.Entities;
using Authentication.Domain.Enums;

namespace Authentication.Infrastructure.Database;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public required DbSet<User> Users { get; set; }
    public required DbSet<RolePermission> RolePermissions { get; set; }
    public required DbSet<UserTotpSecret> UserTotpSecrets { get; set; }
    public required DbSet<Configuration> Configurations { get; set; }
    public required DbSet<EmailWhitelist> EmailWhitelists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User Entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(200);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.KeycloakUserId).IsUnique();
            entity.HasIndex(u => u.IsDeleted).HasFilter("IsDeleted = false");
        });

        // Configure UserTotpSecret Entity
        modelBuilder.Entity<UserTotpSecret>(entity =>
        {
            entity.ToTable("UserTotpSecrets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.KeycloakUserId).IsRequired();
            entity.Property(x => x.Base32Secret).IsRequired();
            entity.HasIndex(x => x.KeycloakUserId).IsUnique();
        });

        // Configure RolePermission Entity
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.KeycloakRoleId).IsRequired();
            entity.Property(b => b.HttpMethod).IsRequired().HasMaxLength(10);
            entity.Property(b => b.ActionId).IsRequired();            
            entity.Property(b => b.EndPoints).IsRequired().HasColumnType("jsonb");
            entity.Property(b => b.Urls).IsRequired().HasColumnType("jsonb");            
            entity.HasIndex(b => new { b.KeycloakRoleId, b.HttpMethod, b.Allowed });
        });

        // Configure Configuration Entity
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.ToTable("Configuration");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MfaType).IsRequired().HasDefaultValue(MfaType.None);
            entity.Property(x => x.ModifiedAt).IsRequired();
        });

        // Configure EmailWhitelist Entity
        modelBuilder.Entity<EmailWhitelist>(b =>
        {
            b.ToTable("EmailWhitelist");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.Property(x => x.Value).HasMaxLength(320).IsRequired();           
            b.HasIndex(x => new { x.Type, x.Value }).IsUnique();
        });

        DataSeeder.Seed(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
