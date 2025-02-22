﻿using DMS.Auth.Application.Interfaces;
using DMS.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.Auth.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public required DbSet<User> Users { get; set; }

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
                entity.Property(u => u.AgencyId).HasMaxLength(100);
            });

            //// Configure AgencyAuthConfig (If needed in database)
            //modelBuilder.Entity<AgencyAuthConfig>(entity =>
            //{
            //    entity.HasKey(a => a.AgencyId);
            //    entity.Property(a => a.KeycloakUrl).IsRequired();
            //    entity.Property(a => a.Realm).IsRequired();
            //});
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
