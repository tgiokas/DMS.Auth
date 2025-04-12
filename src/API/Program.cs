using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using Authentication.Api.Middlewares;
using Authentication.Application;
using Authentication.Application.Interfaces;

using Authentication.Infrastructure;
using Authentication.Infrastructure.ExternalServices;
using Authentication.Infrastructure.Caching;
using Authentication.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
//if (Log.Logger == null || Log.Logger.GetType() == typeof(LoggerConfiguration))
//{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateLogger();
//}

Log.Information("Configuring is starting...");

builder.Host.UseSerilog();

// Add memory cache
builder.Services.AddMemoryCache();

builder.Services.AddScoped<ITotpCacheService, TotpCacheService>();
builder.Services.AddScoped<ISmsCacheService, SmsCacheService>();
builder.Services.AddSingleton<IEmailCacheService, EmailCacheService>();
builder.Services.AddSingleton<IPasswordForgotCacheService, ForgotPasswordCacheService>();

// Add Application services
builder.Services.AddApplicationServices();

// Register Database Context
builder.Services.AddInfrastructureServices(builder.Configuration, "postgresql");

// HttpClient for Keycloak
builder.Services.AddHttpClient<IKeycloakClient, KeycloakClient>(client =>
{
    var baseUrl = builder.Configuration["Keycloak:BaseUrl"];
    if (string.IsNullOrEmpty(baseUrl))
    {
        throw new ArgumentNullException("Keycloak:BaseUrl", "Keycloak BaseUrl configuration is missing or empty.");
    }
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient<ISmsSender, SmsSenderService>();
builder.Services.AddHttpClient<IEmailSender, EmailSenderService>();
builder.Services.AddHttpClient<IKeycloakClient, KeycloakClient>();

builder.Services.AddControllers();

//builder.Services.AddEndpointsApiExplorer();

// Configure Authentication & Keycloak JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:ClientId"];
        options.RequireHttpsMetadata = false; // Only for local dev
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"],            
            ValidateAudience = true,
            ValidAudiences = new[] { "dms-auth-app"}, 
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            //RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            //RoleClaimType = "realm_access.roles",
            //NameClaimType = "preferred_username"
        };

        // Extract roles from `realm_access` JSON object using System.Text.Json
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                MapKeycloakRolesToRoleClaims(context);
                return Task.CompletedTask;
            }
        };

    });

//Role-Based Access Control (RBAC) policies with role claim mapping
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"))
    .AddPolicy("UserOnly", policy => policy.RequireRole("user"))
    .AddPolicy("AdminOrUser", policy => policy.RequireRole("admin", "user"));

var app = builder.Build();

Log.Information("Application is starting...");

if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    //using var scope = app.Services.CreateScope();
    //var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //dbContext.Database.Migrate();
}

app.UseMiddleware<LogMiddleware>(); // Enable Serilog logging for API requests

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

void MapKeycloakRolesToRoleClaims(TokenValidatedContext context)
{
    var user = context.Principal;
    var identity = user?.Identity as ClaimsIdentity;

    var realmAccessClaim = identity?.FindFirst("realm_access");
    if (realmAccessClaim == null)
    {
        Console.WriteLine("realm_access claim not found in token.");
        return;
    }

    try
    {
        using var doc = JsonDocument.Parse(realmAccessClaim.Value);
        if (doc.RootElement.TryGetProperty("roles", out var roles))
        {
            Console.WriteLine("Extracted Roles from Token:");
            foreach (var role in roles.EnumerateArray())
            {
                string? roleValue = role.GetString()?.ToLower();
                if (roleValue != null)
                {
                    identity?.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                    Console.WriteLine($" - {roleValue}");
                }
            }
        }
        else
        {
            Console.WriteLine("No roles found in realm_access");
        }
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Failed to parse realm_access: {ex.Message}");
    }
}

//void MapKeycloakRolesToRoleClaims2(TokenValidatedContext context)
//{
//    //var resourceAccess = JObject.Parse(context.Principal.FindFirst("resource_access").Value);
//    //var clientResource = resourceAccess[context.Principal.FindFirstValue("aud")];
//    var clientRoles = context.Principal.Claims.Where(w => w.Type == "user_realm_roles").ToList();
//    var claimsIdentity = context.Principal.Identity as ClaimsIdentity;
//    if (claimsIdentity == null)
//    {
//        return;
//    }

//    foreach (var clientRole in clientRoles)
//    {
//        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, clientRole.Value));
//    }
//}