using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using RabbitMQ.Client;

using DMS.Auth.Application.DependencyInjection;
using DMS.Auth.Application.Interfaces;
using DMS.Auth.Application.Mappings;
using DMS.Auth.Infrastructure.DependencyInjection;
using DMS.Auth.Infrastructure.ExternalServices;
using DMS.Auth.Infrastructure.Persistence;
using Serilog;
using DMS.Auth.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add memory cache
builder.Services.AddMemoryCache();

//builder.Services.AddScoped<IKeycloakClient, KeycloakClient>();
builder.Services.AddScoped<ITotpCacheService, TotpCacheService>();

// Add services to the container.
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

//builder.Services.AddHttpClient<IKeycloakClient, KeycloakClient>();
//builder.Services.AddScoped<IKeycloakClient, KeycloakClient>();

// RabbitMQ Connection
//builder.Services.AddSingleton(sp =>
//{
//    var factory = new ConnectionFactory
//    {
//        HostName = builder.Configuration["RabbitMQ:HostName"],
//        UserName = builder.Configuration["RabbitMQ:UserName"],
//        Password = builder.Configuration["RabbitMQ:Password"]
//    };
//    return (IConnection)factory.CreateConnectionAsync();
//});


// Audit Event Publisher
//builder.Services.AddScoped<IAuditEventPublisher, RabbitMqAuditEventPublisher>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen();

//builder.Services.AddKeycloakAuthentication(Config);

//builder.Services.AddSwaggerGen(x =>
//{
//    x.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });
//    x.AddSecurityDefinition("Bearer ", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header using the Bearer scheme.",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer "
//    });
//    x.AddSecurityRequirement(new OpenApiSecurityRequirement
//                {
//                    {
//                        new OpenApiSecurityScheme
//                        {
//                            Reference = new OpenApiReference
//                            {
//                                Type = ReferenceType.SecurityScheme,
//                                Id = "Bearer "
//                            },
//                            Scheme = "oauth2",
//                            Name = "Bearer ",
//                            In = ParameterLocation.Header
//                        },
//                        new List<string>()
//                    }
//                });
//});

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
            ValidAudiences = new[] { "dms-auth-app"}, // Allow multiple audiences
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

//Ensure Role-Based Access Control (RBAC) uses the correct claim mapping
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"))
    .AddPolicy("UserOnly", policy => policy.RequireRole("user"))
    .AddPolicy("AdminOrUser", policy => policy.RequireRole("admin", "user"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //    app.UseSwagger();
    //    app.UseSwaggerUI();
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

//app.UseSerilogRequestLogging(); // Enable Serilog request logging

app.UseMiddleware<LogMiddleware>(); // Enable Serilog request logging

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

//app.UseStaticFiles(); // Enable serving static files

//app.UseRouting();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapGet("/", async context =>
//    {
//        context.Response.Redirect("htmlpage.html");
//    });
//    endpoints.MapControllers();
//});


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


