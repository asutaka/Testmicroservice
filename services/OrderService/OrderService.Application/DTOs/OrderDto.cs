using OrderService.Domain.Entities;

namespace OrderService.Application.DTOs;

public record OrderDto(
    Guid Id,
    Guid UserId,
    OrderStatus Status,
    string StatusName,
    decimal TotalAmount,
    string Currency,
    IEnumerable<OrderItemDto> Items,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OrderItemDto(
    Guid Id,
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);
