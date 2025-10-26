using Authentication.Application.Interfaces;
using Authentication.Application.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {       
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<ISmsVerificationService, SmsVerificationService>();
        services.AddScoped<BusinessRuleService, BusinessRuleService>();

        return services;
    }
}

