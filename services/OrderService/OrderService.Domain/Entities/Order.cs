using OrderService.Domain.DomainEvents;
using OrderService.Domain.ValueObjects;
using SharedKernel.Entities;

namespace OrderService.Domain.Entities;

public sealed class Order : BaseEntity<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Of(0);
    public string? Notes { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // For EF Core
    private Order() { }

    private Order(Guid id, Guid userId, string? notes)
    {
        Id = id;
        UserId = userId;
        Status = OrderStatus.Pending;
        Notes = notes;
    }

    public static Order Create(Guid userId, string? notes = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        var order = new Order(Guid.NewGuid(), userId, notes);
        return order;
    }

    public void AddItem(string productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to a non-pending order.");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice <= 0)
            throw new ArgumentException("Unit price must be positive.", nameof(unitPrice));

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem is not null)
        {
            // Replace with updated quantity total
            _items.Remove(existingItem);
        }

        _items.Add(new OrderItem(Id, productId, productName, quantity, unitPrice));
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in status '{Status}'.");
        if (!_items.Any())
            throw new InvalidOperationException("Cannot confirm an order with no items.");

        Status = OrderStatus.Confirmed;
        SetUpdatedAt();

        AddDomainEvent(new OrderCreatedDomainEvent(Id, UserId, TotalAmount.Amount, TotalAmount.Currency));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new InvalidOperationException($"Cannot cancel order in status '{Status}'.");

        Status = OrderStatus.Cancelled;
        Notes = reason;
        SetUpdatedAt();
    }

    public void MarkAsShipped()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be shipped.");
        Status = OrderStatus.Shipped;
        SetUpdatedAt();
    }

    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be marked as delivered.");
        Status = OrderStatus.Delivered;
        SetUpdatedAt();
    }

    private void RecalculateTotal()
    {
        var total = _items.Sum(i => i.TotalPrice);
        TotalAmount = Money.Of(total);
    }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
