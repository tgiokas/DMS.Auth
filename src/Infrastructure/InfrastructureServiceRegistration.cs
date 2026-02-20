using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;
using Authentication.Infrastructure.Repositories;

namespace Authentication.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration, string databaseProvider)
    {
        var connectionString = configuration["AUTH_DB_CONNECTION"];

        NpgsqlDataSource? dataSource = null;
        if (databaseProvider.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            dataSource = dataSourceBuilder.Build();
        }

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            switch (databaseProvider.ToLower())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;

                case "postgresql":                    
                    options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention();
                    break;

                case "sqlite":
                    //options.UseSqlite(connectionString);                     
                    break;

                default:
                    throw new ArgumentException($"Unsupported database provider: {databaseProvider}");
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITotpRepository, TotpRepository>();
        services.AddScoped<IRolePermissionRepo, RolePermissionRepo>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IEmailWhitelistRepository, EmailWhitelistRepository>();

        return services;
    }
}
