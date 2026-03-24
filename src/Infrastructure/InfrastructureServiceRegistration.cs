using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Community.Microsoft.Extensions.Caching.PostgreSql;
using Npgsql;

using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;
using Authentication.Infrastructure.Repositories;
using Authentication.Infrastructure.Caching;
using Authentication.Infrastructure.ExternalServices;
using Authentication.Infrastructure.Messaging;

namespace Authentication.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, string databaseProvider)
    {
        // Add Database Context
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
            switch (databaseProvider.ToLower())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;

                case "postgresql":
                    // Pin history table to "public" to avoid schema mismatch with Zalando prepared DBs (default schema "data")
                    // and Npgsql existence check (see npgsql/efcore.pg#2787, #2878, #3354)
                    options.UseNpgsql(dataSource, npgsql =>
                    {
                        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                    }).UseSnakeCaseNamingConvention();
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

        // Add caching services
        services.AddMemoryCache();
        services.AddScoped<ITotpCache, TotpCache>();
        services.AddScoped<IEmailCache, EmailCache>();
        services.AddScoped<ISmsCache, SmsCache>();
        services.AddScoped<IPasswordResetCache, PasswordResetCache>();

        // Add HttpClient services
        services.AddHttpClient<IKeycloakClientAuthentication, KeycloakClientAuthentication>();
        services.AddHttpClient<IKeycloakClientUser, KeycloakClientUser>();
        services.AddHttpClient<IKeycloakClientRole, KeycloakClientRole>();

        // Register Kafka-based SMS sender
        services.AddSingleton<ISmsSender, KafkaSmsSender>();

        // Register Kafka-based Email sender
        services.AddSingleton<IEmailSender, KafkaEmailSender>();

        // Register Kafka KafkaPublisher
        services.AddSingleton<IMessagePublisher, KafkaPublisher>();

        // Register Login Lockout Service
        services.AddScoped<IAuthLockoutService, AuthLockoutService>();

        // Register Distributed Cache PostgreSql
        services.AddDistributedPostgreSqlCache(options =>
        {
            options.ConnectionString = configuration["AUTH_DB_CONNECTION"];
            options.SchemaName = "public";
            options.TableName = "CacheEntries";
        });

        // Add Error Catalog Path
        var path = Path.Combine(Environment.CurrentDirectory, "errors.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"errors.json not found at: {path}");
        
        var errorcat = ErrorCatalog.LoadFromFile(path);
        services.AddSingleton<IErrorCatalog>(errorcat);

        return services;
    }
}
