using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Community.Microsoft.Extensions.Caching.PostgreSql;
using Serilog;

using Authentication.Api.Middlewares;
using Authentication.Api.Services;
using Authentication.Application;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Infrastructure;
using Authentication.Infrastructure.Caching;
using Authentication.Infrastructure.Database;
using Authentication.Infrastructure.ExternalServices;
using Authentication.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Register health check services
builder.Services.AddHealthChecks();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

Log.Information("Configuration is starting...");

builder.Host.UseSerilog();

// Add memory cache
builder.Services.AddMemoryCache();

builder.Services.AddScoped<ITotpCache, TotpCache>();
builder.Services.AddScoped<IEmailCache, EmailCache>();
builder.Services.AddScoped<ISmsCache, SmsCache>();
builder.Services.AddScoped<IPasswordResetCache, PasswordResetCache>();

builder.Services.AddHttpClient<IKeycloakClientAuthentication, KeycloakClientAuthentication>();
builder.Services.AddHttpClient<IKeycloakClientUser, KeycloakClientUser>();
builder.Services.AddHttpClient<IKeycloakClientRole, KeycloakClientRole>();

// Add Application services
builder.Services.AddApplicationServices();

// Register Database Context
builder.Services.AddInfrastructureServices(builder.Configuration, "postgresql");

// Register Kafka-based SMS sender
builder.Services.AddSingleton<ISmsSender, KafkaSmsSender>();

// Register Kafka-based Email sender
builder.Services.AddSingleton<IEmailSender, KafkaEmailSender>();

builder.Services.AddSingleton<IMessagePublisher, KafkaPublisher>();

builder.Services.AddDistributedPostgreSqlCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.SchemaName = "public";
    options.TableName = "CacheEntries";
});

builder.Services.AddSingleton<KeycloakRoleMapper>();

// ---------- Error Catalog Path ----------
var path = Path.Combine(builder.Environment.ContentRootPath, "errors.json");
if (!File.Exists(path))
    throw new FileNotFoundException($"errors.json not found at: {path}");
Log.Information("Using error catalog at: {Path}", path);
var errorcat = ErrorCatalog.LoadFromFile(path);
builder.Services.AddSingleton<IErrorCatalog>(errorcat);

builder.Services.AddControllers();

// Configure Authentication & Keycloak JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:ClientId"];
        options.RequireHttpsMetadata = bool.Parse(builder.Configuration["Keycloak:RequireHttpsMetadata"] ?? "false");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Keycloak:Authority"],            
            ValidateAudience = true,
            ValidAudiences = [builder.Configuration["Keycloak:ClientId"]],
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
                var roleMapper = context.HttpContext.RequestServices.GetRequiredService<KeycloakRoleMapper>();
                roleMapper.MapRolesToClaims(context);
                return Task.CompletedTask;
            }
        };

    });

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin();
        policyBuilder.AllowAnyMethod();
        policyBuilder.AllowAnyHeader();
    });
});

// builder.WebHost.UseUrls("http://0.0.0.0:80");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Expose a simple health endpoint at /health
app.MapHealthChecks("/health");

Log.Information("Application is starting...");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
dbContext.Database.Migrate();
Log.Information("Database migrations applied (if any).");

app.UseCors("CorsPolicy");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<LogMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();