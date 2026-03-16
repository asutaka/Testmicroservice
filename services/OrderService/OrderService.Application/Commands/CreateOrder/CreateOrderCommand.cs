using MediatR;
using SharedKernel.Common;
using OrderService.Application.DTOs;

namespace OrderService.Application.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid UserId,
    IEnumerable<CreateOrderItemDto> Items,
    string? Notes = null
) : IRequest<Result<OrderDto>>;

public record CreateOrderItemDto(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);
