using SharedKernel.Events;

namespace OrderService.Domain.DomainEvents;

public sealed class OrderCreatedDomainEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid UserId { get; }
    public decimal TotalAmount { get; }
    public string Currency { get; }

    public OrderCreatedDomainEvent(Guid orderId, Guid userId, decimal totalAmount, string currency)
    {
        OrderId = orderId;
        UserId = userId;
        TotalAmount = totalAmount;
        Currency = currency;
    }
}
