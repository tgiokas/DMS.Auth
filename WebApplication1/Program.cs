using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/DMSRealm";
        options.Audience = "dms-auth-app";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://localhost:8080/realms/DMSRealm",
            ValidateAudience = true,
            ValidAudience = "dms-auth-app",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    options.AddPolicy("AdminOrUser", policy => policy.RequireRole("Admin", "User"));
});

builder.Services.AddControllers();

var app = builder.Build();

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
