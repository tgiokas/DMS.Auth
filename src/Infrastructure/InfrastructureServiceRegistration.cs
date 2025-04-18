﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;
using Authentication.Infrastructure.Repositories;

namespace Authentication.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, string databaseProvider)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            switch (databaseProvider.ToLower())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;

                case "postgresql":
                    options.UseNpgsql(connectionString);
                    break;

                case "sqlite":
                    options.UseSqlite(connectionString);                   
                    break;

                default:
                    throw new ArgumentException($"Unsupported database provider: {databaseProvider}");
            }
        });

        //services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ITotpRepository, TotpSecretRepository>();

        return services;
    }
}
