using EventBus.Extensions;
using Microsoft.OpenApi.Models;
using Observability.Extensions;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Infrastructure;
using UserService.Contracts.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderService API",
        Version = "v1",
        Description = "Microservice for order management. Calls UserService via gRPC."
    });
});

// gRPC server
builder.Services.AddGrpc(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly));

// Infrastructure
var connectionString = builder.Configuration.GetConnectionString("OrderDb");
builder.Services.AddInfrastructure(connectionString);

// gRPC client → UserService
var userServiceGrpcUrl = builder.Configuration["GrpcClients:UserService"] ?? "http://localhost:5101";
builder.Services.AddGrpcClient<UserGrpc.UserGrpcClient>(o => o.Address = new Uri(userServiceGrpcUrl));

// Event Bus
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddRabbitMQEventBus(rabbitMqHost);

// Observability
builder.Services.AddObservability("OrderService");
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseObservability();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
