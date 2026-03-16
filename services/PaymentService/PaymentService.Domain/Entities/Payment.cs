using SharedKernel.Entities;

namespace PaymentService.Domain.Entities;

public sealed class Payment : BaseEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public string? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }

    // For EF Core
    private Payment() { }

    private Payment(Guid id, Guid orderId, Guid userId, decimal amount, string currency)
    {
        Id = id;
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
    }

    public static Payment Create(Guid orderId, Guid userId, decimal amount, string currency = "USD")
        => new(Guid.NewGuid(), orderId, userId, amount, currency);

    public void MarkAsProcessing() { Status = PaymentStatus.Processing; SetUpdatedAt(); }

    public void MarkAsCompleted(string transactionId)
    {
        Status = PaymentStatus.Completed;
        TransactionId = transactionId;
        SetUpdatedAt();
    }

    public void MarkAsFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        SetUpdatedAt();
    }

    public void Refund()
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Only completed payments can be refunded.");
        Status = PaymentStatus.Refunded;
        SetUpdatedAt();
    }
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
