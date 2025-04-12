using Authentication.Application.Interfaces;
using Authentication.Application.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {       
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IPasswordForgotService, PasswordForgotService>();
        services.AddScoped<ISmsVerificationService, SmsVerificationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();        

        return services;
    }
}

