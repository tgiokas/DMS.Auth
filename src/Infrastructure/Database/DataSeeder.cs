using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Enums;

namespace Authentication.Infrastructure.Database
{
    public static class DataSeeder
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            // Initial Data for Configuration MfatType: None
            modelBuilder.Entity<Configuration>().HasData(new Configuration
            {
                Id = 1,
                MfaType = MfaType.None,
                ModifiedAt = DateTime.UtcNow
            });

            // Initial Data for Users
            modelBuilder.Entity<User>().HasData(
                 new User
                 {
                     Id = 1,
                     KeycloakUserId = Guid.Parse("255938e1-6a58-4ceb-b7b7-79670966958f"),
                     Username = "testuser",
                     MfaType = MfaType.None,
                     IsAdmin = false,
                     IsDeleted = false,
                     CreatedAt = DateTime.UtcNow
                 }

            );
        }
    }

}
