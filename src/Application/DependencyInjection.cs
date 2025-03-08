﻿using DMS.Auth.Application.Interfaces;
using DMS.Auth.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {        
        //services.AddAutoMapper(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }
}

