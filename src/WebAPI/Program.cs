using DMS.Auth.Domain.Interfaces;
using DMS.Auth.Infrastructure.Audit;
using DMS.Auth.Infrastructure.Keycloak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices();

// Register Database Context
builder.Services.AddInfrastructureServices(builder.Configuration, "sqlserver");

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// RabbitMQ Connection
builder.Services.AddSingleton(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:HostName"],
        UserName = builder.Configuration["RabbitMQ:UserName"],
        Password = builder.Configuration["RabbitMQ:Password"]
    };
    return (IConnection)factory.CreateConnectionAsync();
});

// Audit Event Publisher
builder.Services.AddScoped<IAuditEventPublisher, RabbitMqAuditEventPublisher>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Authentication & Keycloak JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:ClientId"];
        options.RequireHttpsMetadata = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

