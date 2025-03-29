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

//builder.Services.AddSwaggerGen();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


//// Protected endpoints with role-based access control
//app.MapGet("/api/admin", [Authorize(Policy = "AdminOnly")] () => "Admin access granted.");

//app.MapGet("/api/user", [Authorize(Policy = "UserOnly")] () => "User access granted.");

//app.MapGet("/api/shared", [Authorize(Policy = "AdminOrUser")] () => "Admin or User access granted.");

//app.MapGet("/api/protected-endpoint", [Authorize] () => "This is a protected resource.");

//app.MapGet("/api/service-to-service", [Authorize] () => "Service-to-Service Authentication successful.");

//app.MapGet("/api/third-party", [Authorize] () => "Third-party API authentication successful.");

//app.MapGet("/api/delegated", [Authorize] () => "Delegated OAuth 2.0 access successful.");

//app.MapGet("/api/api-key-protected", (context) =>
//{
//    var apiKey = context.Request.Headers["x-api-key"];
//    if (apiKey == "YOUR_GENERATED_API_KEY")
//    {
//        return context.Response.WriteAsync("API Key authentication successful.");
//    }
//    context.Response.StatusCode = 403;
//    return context.Response.WriteAsync("Invalid API Key.");
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
