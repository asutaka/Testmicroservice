using EventBus.Extensions;
using EventBus.Interfaces;
using Microsoft.OpenApi.Models;
using Observability.Extensions;
using OrderService.Contracts.Events;
using PaymentService.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PaymentService API",
        Version = "v1",
        Description = "Microservice for payment processing. Listens to OrderCreated events."
    });
});

// Event Bus (consumes OrderCreated from RabbitMQ)
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddRabbitMQEventBus(rabbitMqHost);

// Register event handler
builder.Services.AddScoped<IIntegrationEventHandler<OrderCreatedIntegrationEvent>, OrderCreatedEventHandler>();

// Observability
builder.Services.AddObservability("PaymentService");
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseObservability();
app.MapControllers();
app.MapHealthChecks("/health");

// Subscribe to events on startup
var eventBus = app.Services.GetRequiredService<IEventBus>();
eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedEventHandler>();

app.Run();


// ──────── Event Handler (inline for simplicity) ────────────────────────────────

/// <summary>
/// Subscribes to OrderCreated events from RabbitMQ and initiates payment processing.
/// In production, this would be in PaymentService.Application/EventHandlers/
/// </summary>
public sealed class OrderCreatedEventHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) => _logger = logger;

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "📦 PaymentService received OrderCreated: OrderId={OrderId}, Amount={Amount} {Currency}",
            @event.OrderId, @event.TotalAmount, @event.Currency);

        // Simulate payment processing
        var payment = Payment.Create(@event.OrderId, @event.UserId, @event.TotalAmount, @event.Currency);
        payment.MarkAsProcessing();

        await Task.Delay(200, cancellationToken); // simulate gateway call

        var transactionId = $"TXN-{Guid.NewGuid():N}";
        payment.MarkAsCompleted(transactionId);

        _logger.LogInformation(
            "✅ Payment completed: TransactionId={TransactionId} for Order {OrderId}",
            transactionId, @event.OrderId);
    }
}
