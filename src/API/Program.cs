using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using Authentication.Api.Middlewares;
using Authentication.Api.Services;
using Authentication.Application;
using Authentication.Application.Configuration;
using Authentication.Infrastructure;
using Authentication.Infrastructure.Database;

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

// Add Application services
builder.Services.AddApplicationServices();

// Add Infrastructure Services 
builder.Services.AddInfrastructureServices(builder.Configuration, "postgresql");

// Bind KeycloakSettings early so we can use it for JWT config
var keycloakSettings = KeycloakSettings.BindFromConfiguration(builder.Configuration);

builder.Services.AddSingleton<KeycloakRoleMapper>();

builder.Services.AddControllers();

// Configure Authentication & Keycloak JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakSettings.Authority;
        options.Audience = keycloakSettings.ClientId;
        options.RequireHttpsMetadata = keycloakSettings.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakSettings.Authority,
            ValidateAudience = true,
            ValidAudiences = [keycloakSettings.ClientId],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
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

// Add Swagger in Development environment only
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

// Auto-migrate
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
