using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/DMSRealm";
        options.Audience = "dms-auth-client";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/DMSRealm",
            ValidateAudience = true,
            ValidAudience = "dms-auth-client",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminOrUser", policy => policy.RequireRole("Admin", "User"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Protected endpoints with role-based access control
app.MapGet("/api/admin", [Authorize(Policy = "AdminOnly")] () => "Admin access granted.");

app.MapGet("/api/user", [Authorize(Policy = "UserOnly")] () => "User access granted.");

app.MapGet("/api/shared", [Authorize(Policy = "AdminOrUser")] () => "Admin or User access granted.");

app.MapGet("/api/protected-endpoint", [Authorize] () => "This is a protected resource.");

app.MapGet("/api/service-to-service", [Authorize] () => "Service-to-Service Authentication successful.");

app.MapGet("/api/third-party", [Authorize] () => "Third-party API authentication successful.");

app.MapGet("/api/delegated", [Authorize] () => "Delegated OAuth 2.0 access successful.");

app.MapGet("/api/api-key-protected", (context) =>
{
    var apiKey = context.Request.Headers["x-api-key"];
    if (apiKey == "YOUR_GENERATED_API_KEY")
    {
        return context.Response.WriteAsync("API Key authentication successful.");
    }
    context.Response.StatusCode = 403;
    return context.Response.WriteAsync("Invalid API Key.");
});

app.Run();
