using EventBus;

namespace OrderService.Contracts.Events;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    string Currency,
    IEnumerable<OrderItemEvent> Items
) : IntegrationEvent;

public sealed record OrderItemEvent(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
