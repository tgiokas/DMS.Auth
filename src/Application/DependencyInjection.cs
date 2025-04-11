using Authentication.Application.Interfaces;
using Authentication.Application.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {       
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        //services.AddScoped<ISmsSender, SmsSenderService>();
        services.AddScoped<ISmsVerificationService, SmsVerificationService>();
        //services.AddScoped<IEmailSender, EmailSenderService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();

        return services;
    }
}

