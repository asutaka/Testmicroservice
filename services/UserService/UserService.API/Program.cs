using EventBus.Extensions;
using FluentValidation;
using Microsoft.OpenApi.Models;
using Observability.Extensions;
using UserService.API.GrpcServices;
using UserService.Application.Commands.CreateUser;
using UserService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UserService API",
        Version = "v1",
        Description = "Microservice responsible for user management"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});

// gRPC
builder.Services.AddGrpc(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

// CQRS – MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateUserCommandHandler).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserCommandValidator>();

// Infrastructure (EF Core + Repositories)
var connectionString = builder.Configuration.GetConnectionString("UserDb");
builder.Services.AddInfrastructure(connectionString);

// Event Bus (RabbitMQ)
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddRabbitMQEventBus(rabbitMqHost);

// Observability
builder.Services.AddObservability("UserService");

builder.Services.AddHealthChecks();

// ─── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseObservability(); // Prometheus /metrics endpoint
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<UserGrpcService>();
app.MapHealthChecks("/health");

app.Run();
